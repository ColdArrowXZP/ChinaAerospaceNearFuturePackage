using EdyCommonTools;
using Expansions. Missions. Editor;
using Expansions. Serenity;
using KSP. Localization;
using KSPAchievements;
using System;
using System. Collections;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public class SampleArmCtrlLogic : MonoBehaviour
    {
        private List<float[]> armMotionRecord = new List<float[]> ();
        private int armRotateSpeed;
        private ArmJoint baseJoint;
        private bool canSetTarget = true;
        private float convergeThreshold = 0.5f;
        private ModuleCASNFP_RoboticArmPart currentArmPart;
        private ArmJoint effectJoint;
        private bool hasPendingTarget = false;
        private RaycastHit hit;
        private bool isPlayingBack = false;
        private bool isResetting = false;
        private float jointLength;
        private List<ArmJoint> joints = new List<ArmJoint> ();
        private int maxUnreachFrame = 1000;
        private Vector3 pendingTargetPos;
        private int playbackIndex = -1;
        private float resetThreshold = 0.5f;
        private CASNFP_SetRocAutoCtrl rocAutoCtrl;
        private Vector3 targetPos;
        private Vector3 targetPosNoramlDir;
        private int unreachFrameCount = 0;
        private Transform workTransform;
        public void Awake ()
        {
            rocAutoCtrl = gameObject. GetComponent<CASNFP_SetRocAutoCtrl> ();
            if ( rocAutoCtrl == null )
            {
                Debug. Log ("程序错误,没有找到嫦娥机械臂控制程序");
                Destroy (this);
            }
        }

        public void Start ()
        {
            currentArmPart = rocAutoCtrl. currentWorkingRoboticArm;
            joints. Clear ();
            joints = currentArmPart. joints;
            baseJoint = currentArmPart. baseJoint;
            effectJoint = currentArmPart. effectJoint;
            workTransform = currentArmPart. workPos;
            jointLength = Vector3. Distance (effectJoint. transform. position, workTransform. position);
            armRotateSpeed = currentArmPart. armSpeed;
        }
        public void Update ()
        {
            if ( !HighLogic. LoadedSceneIsFlight )
            {
                return;
            }
            if ( currentArmPart. vessel. srf_velocity. magnitude > 0.1d )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"载具移动，无法取样,机械臂回收"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                isResetting = true;
                return;
            }
            if ( isResetting )
            {
                ResetArmSmooth ();
                return;
            }
            if ( hit. collider != null )
            {
                IK (targetPos);
                // 收敛性判断，每帧执行
                float dist = Vector3. Distance (workTransform. position, targetPos);
                if ( dist > convergeThreshold )
                {
                    unreachFrameCount++;
                    if ( unreachFrameCount > maxUnreachFrame )
                    {
                        Debug. LogWarning ("取样点位置过远，超出机械臂工作范围: " + targetPos + "当前执行帧数：" + unreachFrameCount);
                        hit = new RaycastHit ();
                        isResetting = true;
                        hasPendingTarget = false;
                        unreachFrameCount = 0;
                    }
                }
                else
                {
                    unreachFrameCount = 0; // 已收敛，计数清零
                    hit = new RaycastHit ();
                    ScreenMessages. PostScreenMessage (
                        Localizer. Format ($"机械臂已到达取样点,开始取样"),
                        2f, ScreenMessageStyle. UPPER_RIGHT);
                    // 触发取样动作,取样过程中不允许设置新目标，取样结束记得把canSetTarget设为true
                    canSetTarget = false;
                }
            }
            if ( !canSetTarget )
            {
                return;
            }
            if ( !Input. GetMouseButtonDown (0) )
                return;
            if ( !TryGetValidSamplePoint (out hit) )
                return;
            else
            {
                targetPosNoramlDir = ( hit. point - FlightGlobals. currentMainBody. transform. position ). normalized;
                targetPos = hit. point + targetPosNoramlDir * jointLength;
                Debug. Log ("targetPos" + targetPos);

                // 设置回归初始状态标志，并保存待处理目标
                isResetting = true;
                pendingTargetPos = hit. point;
                hasPendingTarget = true;
            }
        }

        private void IK (Vector3 targetPos)
        {
            // IK迭代：只通过旋转驱动，不直接赋值位置
            for ( int i = 0 ; i < armRotateSpeed ; i++ )
            {
                // 计算effectJoint的目标位置（始终在targetPos正上方，距离为jointLength）
                Vector3 effectTargetPos = targetPos + Vector3. up * jointLength;

                // 只让前三个关节参与IK，使effectJoint趋近于effectTargetPos
                Vector3 targetLocalPos = baseJoint. transform. InverseTransformPoint (effectTargetPos);
                Vector3 effectLocalPos = baseJoint. transform. InverseTransformPoint (effectJoint. transform. position);
                float distanceToTarget = Vector3. Distance (targetLocalPos, effectLocalPos);
                if ( distanceToTarget < 0.01f )
                    break;

                for ( int j = joints. Count - 2 ; j >= 0 ; j-- )
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
                // 记录当前帧所有关节角度
                float[] angles = new float[joints. Count];
                for ( int k = 0 ; k < joints. Count ; k++ )
                    angles[k] = joints[k]. currentAngle;
                armMotionRecord. Add (angles);
            }
            for ( int i = 0 ; i < armRotateSpeed ; i++ )
            {
                Vector3 workLocalPos = baseJoint. transform. InverseTransformPoint (workTransform. position);
                Vector3 targetLocalPos = baseJoint. transform. InverseTransformPoint (pendingTargetPos);
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

        private void ResetArmSmooth ()
        {
            if ( armMotionRecord. Count == 0 )
            {
                // 没有展开轨迹，不执行回放，直接复位，并处理待处理目标
                isResetting = false;
                if ( hasPendingTarget )
                {
                    IK (targetPos);
                    hasPendingTarget = false;
                }
                return;
            }
            // 有展开轨迹，执行回放
            // 每帧回放多步，加快回收速度
            int stepsPerFrame = armRotateSpeed; // 可根据需要调整
            for ( int step = 0 ; step < stepsPerFrame && playbackIndex >= 0 ; step++ )
            {
                float[] angles = armMotionRecord[playbackIndex];
                for ( int i = 0 ; i < joints. Count ; i++ )
                {
                    joints[i]. SetAngle (angles[i]);
                }
                playbackIndex--;
            }

            if ( playbackIndex < 0 )
            {
                isPlayingBack = false;
                armMotionRecord. Clear ();
                isResetting = false;
                if ( hasPendingTarget )
                {
                    IK (targetPos);
                    hasPendingTarget = false;
                }
            }
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