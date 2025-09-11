using EdyCommonTools;
using Expansions. Serenity;
using KSP. Localization;
using System;
using System. Collections;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
     public class SampleArmCtrlLogic:MonoBehaviour
    {
        CASNFP_SetRocAutoCtrl rocAutoCtrl;
        List<ArmPartJointInfo> currentArmParts;
        private bool isResetting = false;
        private float resetThreshold = 0.5f; // 允许的误差
        private Vector3 pendingTargetPos;
        private bool hasPendingTarget = false;
        private int unreachFrameCount = 0;
        private int maxUnreachFrame = 1000; // 连续未收敛帧数阈值
        private float convergeThreshold = 0.5f; // 收敛距离阈值
        ArmPartJointInfo effectJoint;
        ArmPartJointInfo baseJoint;
        RaycastHit terrainHit;
        //private event Action<bool> isMoving;
        //public bool IsCalCom
        //{
        //    get { return isCalCom; }
        //    private set 
        //    {
        //        if ( isCalCom != value )
        //            isCalCom = value; 
        //            isMoving?.Invoke (isCalCom);
        //    }
        //}

        public void Awake ()
        {
            rocAutoCtrl = gameObject.GetComponent<CASNFP_SetRocAutoCtrl>();
            if ( rocAutoCtrl == null )
            {
                Debug. Log ("程序错误,没有找到嫦娥机械臂控制程序");
                Destroy(this);
            }
        }
        public void Start ()
        {
            currentArmParts = rocAutoCtrl. currentWorkingRoboticArm;
            effectJoint = currentArmParts[currentArmParts.Count - 1];
            baseJoint = currentArmParts[0];
            for ( int i = 0 ; i < currentArmParts. Count ; i++ )
            {
                currentArmParts[i].servoHinge. currentAngle = currentArmParts[i]. servoHinge.targetAngle;
            }
        }


        public void OnDestroy ()
        {
            if ( sphere != null )
            {
                Destroy (sphere);
            }
        }
        //步骤：1、获取机械臂长度，2、获取机械臂基座位置地形高度，3、计算出机械臂工作范围半径，4、设置一个绿色圆环供玩家参考取样点，5、获取鼠标点击事件，6、计算取样点位置，7、执行取样动作。
        private bool TryGetValidSamplePoint (out RaycastHit terrainHit)
        {
            var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);
            if ( Physics. Raycast (ray, out terrainHit, Mathf. Infinity) )
            {
                int num = terrainHit. collider. gameObject. layer;
                if ( num != 10 && num !=15)
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
        private IEnumerator ResetArmSmooth ()
        {
            bool allReached;
            yield return new WaitUntil (() =>
            {
                allReached = true;
                foreach ( var joint in currentArmParts )
                {
                    joint. servoHinge. targetAngle = joint. servoHinge. launchPosition;
                    if ( Mathf. Abs (joint. servoHinge. currentAngle - joint. servoHinge. launchPosition) > resetThreshold )
                        allReached = false;
                }
                return allReached; 
            });
            isResetCom = true;
        }
        bool isResetCom = false;
        public void Update ()
        {
            if ( !HighLogic. LoadedSceneIsFlight)
            {
                return;
            }
            if ( currentArmParts[0]. vessel. srf_velocity. magnitude > 0.1d )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"载具移动中，无法取样"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                return;
            }
            if ( isResetting && !isResetCom)
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"机械臂重置中，请稍候"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                StartCoroutine (ResetArmSmooth ());
                isResetting = false;
                return;
            }
            if ( hasPendingTarget && isResetCom)
            {
                IK (pendingTargetPos);
                // 收敛性判断，每帧执行
                float dist = Vector3. Distance (effectJoint. workPosTransform. position, pendingTargetPos);
                if ( dist > convergeThreshold )
                {
                    unreachFrameCount++;
                    if ( unreachFrameCount > maxUnreachFrame )
                    {
                        Debug. LogWarning ("取样点位置过远，超出机械臂工作范围: " + pendingTargetPos);
                        isResetting = true;
                        isResetCom = false;
                        hasPendingTarget = false;
                        Destroy (sphere);
                        unreachFrameCount = 0;
                    }
                }
                else
                {
                    unreachFrameCount = 0; // 已收敛，计数清零
                    Debug. Log ("机械臂已收敛到目标位置，准备取样: " + pendingTargetPos);
                    isResetting = true;
                    isResetCom = false;
                    hasPendingTarget = false;
                    Destroy (sphere);
                    // 在这里执行取样动作
                }
            }
            if ( !Input. GetMouseButtonDown (0) )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"请选择取样地点"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                return;
            }
            if ( !TryGetValidSamplePoint (out terrainHit) )
            {
                return;
            }
            else
            {
                pendingTargetPos = terrainHit.point;
                //在targetPoint位置生成一个黄色球体，表示取样点
                SetTargetSp (pendingTargetPos);
                // 设置回归初始状态标志，并保存待处理目标
                isResetting = true;
                isResetCom = false;
                hasPendingTarget = true;
            }
        }
        //计算目标点的法向
        private Vector3 CalSurfaceNomal (Vector3 point)
        {

            Ray ray = new Ray (point + Vector3. up * 0.5f, Vector3. down);
            
            if ( Physics. Raycast (ray, out RaycastHit hitInfo, 1f) )
            {
                return hitInfo. normal;
            }
            return Vector3. up; // 默认法线向上
        }
        private void IK (Vector3 targetPos)
        {
            Debug. Log ("进入了IK计算");
            float groundY = targetPos. y; // 地面高度为鼠标点击位置的y
            if ( targetPos. y < groundY )
                targetPos. y = groundY;

            // 计算effectJoint与workTransform的长度
            float jointLength = Vector3. Distance (effectJoint.transform. position, effectJoint. workPosTransform. position);

            // IK迭代：只通过旋转驱动，不直接赋值位置
            for ( int i = 0 ; i < 10 ; i++ )
            {
                // 计算effectJoint的目标位置（始终在targetPos正上方，距离为jointLength）
                Vector3 effectTargetPos = targetPos + Vector3. up * jointLength;

                // 只让前三个关节参与IK，使effectJoint趋近于effectTargetPos
                Vector3 targetLocalPos = baseJoint. transform. InverseTransformPoint (effectTargetPos);
                Vector3 effectLocalPos = baseJoint. transform. InverseTransformPoint (effectJoint. transform. position);
                float distanceToTarget = Vector3. Distance (targetLocalPos, effectLocalPos);
                if ( distanceToTarget < 0.01f )
                    break;

                for ( int j = currentArmParts. Count - 2 ; j >= 0 ; j-- )
                {
                    effectLocalPos = baseJoint. transform. InverseTransformPoint (effectJoint. transform. position);
                    Vector3 toTargetLocal = ( targetLocalPos - effectLocalPos ). normalized;
                    Vector3 toJointLocal = ( effectLocalPos - baseJoint. transform. InverseTransformPoint (currentArmParts[j]. transform. position) ). normalized;
                    float cosAngle = Vector3. Dot (toTargetLocal, toJointLocal);
                    if ( cosAngle > 0.9999f )
                        continue;
                    float angle = Mathf. Acos (cosAngle) * Mathf. Rad2Deg;
                    Vector3 cross = Vector3. Cross (toJointLocal, toTargetLocal);
                    if ( Vector3. Dot (cross, currentArmParts[j].servoHinge.GetMainAxis()) < 0 )
                        angle = -angle;
                    currentArmParts[j].servoHinge.targetAngle = currentArmParts[j].servoHinge. currentAngle + angle;
                }
            }
            for ( int i = 0 ; i < 10 ; i++ )
            {
                Vector3 workLocalPos = baseJoint. transform. InverseTransformPoint (effectJoint. workPosTransform. position);
                Vector3 targetLocalPos = baseJoint. transform. InverseTransformPoint (targetPos);
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
                if ( Vector3. Dot (cross, effectJoint.servoHinge.GetMainAxis()) < 0 )
                    angle = -angle;
                effectJoint.servoHinge.targetAngle =  effectJoint.servoHinge. currentAngle + angle;
            }
        }
        GameObject sphere;
        private void SetTargetSp (Vector3 targetPoint)
        {
            if ( sphere == null )
            {
                sphere = GameObject. CreatePrimitive (PrimitiveType. Sphere);
                Destroy (sphere. GetComponent<SphereCollider> ());
                sphere. transform. position = targetPoint;
                sphere. transform. localScale = new Vector3 (0.1f, 0.1f, 0.1f);
                Material material = sphere.GetComponent<MeshRenderer>().material = new Material (Shader. Find ("KSP/Particles/Additive"));
                material. color = Color. red;
            }
            else
            {
                sphere. transform. position = targetPoint;
            }
            
        }
    }
}
