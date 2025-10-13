using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using KSP. Localization;
using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UniLinq;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public class RoboticArmController : MonoBehaviour
    {
        public ModuleCASNFP_RoboticArmPart controlledArm;
        private ArmJoint[] joints;
        private ArmJoint baseJoint;
        private ArmJoint effectJoint;
        private Transform workTransform;
        private float jointLength;
        private Vector3 effectJointTargetPos;
        private Vector3 workTransformTargetPos;
        private Vector3 targetPosNoramlDir;
        private bool hasTargetPos = false;
        private bool isInitialized = false;
        public void Start ()
        {
            if ( controlledArm == null )
            {
                CASNFPLogger. Instance. Log ("RoboticArmController controlledArm is null, please assign it in the inspector");
                Destroy (this);
                return;
            }

            InitializeArm ();
            isInitialized = true;
        }

        private void InitializeArm ()
        {
            joints = controlledArm. joints;
            if ( joints == null || joints. Length == 0 )
            {
                CASNFPLogger. Instance. Log ("RoboticArmController: No joints found");
                return;
            }
            baseJoint = joints[0];
            effectJoint = joints[joints. Length - 1];
            workTransform = controlledArm. workPos;
            jointLength = Vector3. Distance (effectJoint. transform. position, workTransform. position);
        }

        public void Update ()
        {
            if ( !isInitialized || controlledArm == null )
                return;

            // 检查载具移动
            if ( controlledArm. vessel. srf_velocity. magnitude > 0.1f )
            {
                HandleVesselMovement ();
                return;
            }
            // 根据状态更新机械臂
            switch ( controlledArm. ArmCurrentState )
            {
                case ArmState. Idle:
                    HandleIdleState ();
                    break;
                case ArmState. Extanding:
                    HandleExtandingState ();
                    break;
                case ArmState. Retracting:
                    HandleRetractingState ();
                    break;
                case ArmState. Doing:
                    HandleDoingState ();
                    break;
            }
        }

        private void HandleVesselMovement ()
        {
            ScreenMessages. PostScreenMessage ("载具移动，机械臂无法工作", 3.0f, ScreenMessageStyle. UPPER_RIGHT);
            if ( controlledArm. ArmCurrentState != ArmState. Idle )
            {
                controlledArm. ArmCurrentState = ArmState. Retracting;
                hasTargetPos = false;
            }
        }

        private void HandleIdleState ()
        {
            if ( !hasTargetPos )
            {
                ScreenMessages. PostScreenMessage ("机械臂待命，可以选择取样地点", 3.0f, ScreenMessageStyle. UPPER_CENTER);
                if ( !Input. GetMouseButtonDown (0) )
                    return;

                if ( TryGetValidSamplePoint (out RaycastHit hit) )
                {
                    SetTargetPosition (hit);
                }
            }
            else
            {
                controlledArm. ArmCurrentState = ArmState. Extanding;
            }
        }

        private void HandleExtandingState ()
        {
            //使用FABRIK算法计算每个关节的角度，更新机械臂的位置,检查机械臂是否到达目标点
            CCDIK(effectJointTargetPos);
            CHeckArrived();
        }
        float convergeThreshold = 0.01f;
        int maxUnreachFrame = 100;
        int unreachFrameCount = 0;
        private void CHeckArrived ()
        {
            float dist = Vector3. Distance (effectJoint. transform. position, effectJointTargetPos);
            if ( dist > convergeThreshold )
            {
                unreachFrameCount++;
                if ( unreachFrameCount >= maxUnreachFrame )
                {
                    Debug. LogWarning ("取样点" + workTransformTargetPos + "无法到达，机械臂自动回收");
                    unreachFrameCount = 0;
                    controlledArm. ArmCurrentState = ArmState. Retracting;
                }
            }
            else
            {
                unreachFrameCount = 0; 
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"机械臂已到达取样点,开始取样"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                controlledArm. ArmCurrentState = ArmState.Doing;
            }
        }

        private int ikSpeed = 15;
        private void CCDIK (Vector3 targetPos)
        {
            // IK迭代：只通过旋转驱动，不直接赋值位置
            for ( int i = 0 ; i < ikSpeed ; i++ )
            {
                // 计算effectJoint的目标位置（始终在targetPos正上方，距离为jointLength）
                //Vector3 effectTargetPos = targetPos + Vector3. up * jointLength;

                // 只让前三个关节参与IK，使effectJoint趋近于effectTargetPos
                Vector3 targetLocalPos = baseJoint. transform. InverseTransformPoint (effectJointTargetPos);
                Vector3 effectLocalPos = baseJoint. transform. InverseTransformPoint (effectJoint. transform. position);
                float distanceToTarget = Vector3. Distance (targetLocalPos, effectLocalPos);
                if ( distanceToTarget < 0.01f )
                    break;

                for ( int j = joints.Length - 2 ; j >= 0 ; j-- )
                {
                    effectLocalPos = baseJoint. transform. InverseTransformPoint (effectJoint. transform. position);
                    Vector3 toTargetLocal = ( targetLocalPos - effectLocalPos ). normalized;
                    Vector3 toJointLocal = ( effectLocalPos - baseJoint. transform. InverseTransformPoint (joints[j]. transform. position) ). normalized;
                    float cosAngle = Vector3. Dot (toTargetLocal, toJointLocal);
                    if ( cosAngle > 0.9999f )
                        continue;
                    float angle = Mathf. Acos (cosAngle) * Mathf. Rad2Deg;
                    Vector3 cross = Vector3. Cross (toJointLocal, toTargetLocal);
                    if ( Vector3. Dot (cross, joints[j]. rotateAxais) < 0 )
                        angle = -angle;
                    joints[j]. SetAngle (joints[j]. currentAngle + angle);
                }
            }
            for ( int i = 0 ; i < ikSpeed ; i++ )
            {
                Vector3 workLocalPos = baseJoint. transform. InverseTransformPoint (workTransform. position);
                Vector3 targetLocalPos = baseJoint. transform. InverseTransformPoint (workTransformTargetPos);
                float distanceToTarget = Vector3. Distance (targetLocalPos, workLocalPos);
                if ( distanceToTarget < 0.01f )
                    break;

                // 只让effectJoint参与
                Vector3 toTargetLocal = ( targetLocalPos - workLocalPos ). normalized;
                Vector3 toJointLocal = ( workLocalPos - baseJoint. transform. InverseTransformPoint (effectJoint. transform. position) ). normalized;
                float cosAngle = Vector3. Dot (toTargetLocal, toJointLocal);
                if ( cosAngle > 0.9999f )
                    continue;
                float angle = Mathf. Acos (cosAngle) * Mathf. Rad2Deg;
                Vector3 cross = Vector3. Cross (toJointLocal, toTargetLocal);
                if ( Vector3. Dot (cross, effectJoint. rotateAxais) < 0 )
                    angle = -angle;
                effectJoint. SetAngle (effectJoint. currentAngle + angle);
            }
        }

        private void HandleRetractingState ()
        {
            // 增加旋转速度
            float speedMultiplier = 3.0f; // 速度倍数

            for ( int i = 0 ; i < joints. Length ; i++ )
            {
                // 直接设置目标角度，不使用插值
                float targetAngle = joints[i]. initialAngle;
                float currentAngle = joints[i]. currentAngle;

                // 计算最短旋转路径
                float angleDiff = targetAngle - currentAngle;
                if ( angleDiff > 180 )
                    angleDiff -= 360;
                if ( angleDiff < -180 )
                    angleDiff += 360;

                // 应用速度倍数
                float newAngle = currentAngle + angleDiff * speedMultiplier * Time. deltaTime;
                joints[i]. SetAngle (newAngle);
            }
        }

        private bool IsRetracted ()
        {
            // 检查所有关节是否都回到了初始位置
            for ( int i = 0 ; i < joints. Length ; i++ )
            {
                // 如果差值大于阈值，说明关节未完全收回
                if ( Mathf. Abs (joints[i]. currentAngle - joints[i]. initialAngle) > 0.01f ) // 0.01度的容差
                {
                    break;
                }
                if ( i == joints. Length - 1 )
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleDoingState ()
        {
            hasTargetPos = false;
            if ( Input. GetKeyDown (KeyCode. Backspace) )
                controlledArm. ArmCurrentState = ArmState. Retracting;
        }

        private void SetTargetPosition (RaycastHit hit)
        {
            targetPosNoramlDir = ( hit. point - FlightGlobals. currentMainBody. transform. position ). normalized;
            effectJointTargetPos = hit. point + targetPosNoramlDir * jointLength;
            workTransformTargetPos = hit. point;
            hasTargetPos = true;
        }
        private bool TryGetValidSamplePoint (out RaycastHit terrainHit)
        {
            var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);
            if ( Physics. Raycast (ray, out terrainHit, Mathf. Infinity) )
            {
                int num = terrainHit. collider. gameObject. layer;
                if ( num != 10 && num != 15 )
                {
                    ScreenMessages. PostScreenMessage (
                        Localizer. Format ($"选择的不是地面点无法取样，请在原地面选点"),
                        2f, ScreenMessageStyle. UPPER_RIGHT);
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
