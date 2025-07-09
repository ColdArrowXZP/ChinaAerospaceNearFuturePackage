using Expansions. Serenity;
using System;
using UnityEngine;
using System. Collections;

namespace ChinaAeroSpaceNearFuturePackage. Parts
{
    public class CASNFP_Arm_CE_SampleArm: CASNFP_RoboticArm
    {
        //机械臂自由度数量
        
        private ModuleResourceHarvester _drillMoudule;
        private Transform _drillTransform;
        private string drillTransformName;
        
        private bool _isMoving = false;
        private Vector3 _targetPosition;
        private bool _isSampling = false;
        

        public bool isBootSucceed = false;
       
        

        private IEnumerator MoveToPosition (Vector3 targetPos)
        {
            _isMoving = true;
            _targetPosition = targetPos;
            ScreenMessages. PostScreenMessage ($"[CASNFP_Arm_SampleArm]Moving to sampling position:{targetPos}",3f);
            //检查目标点距离是否在范围内
            if ( !IsPositionReachable (targetPos) )
            {
                ScreenMessages. PostScreenMessage ("[CASNFP_Arm_SampleArm]Target position out of reach", 5f);
                _isMoving = false;
                yield break;
            }
            //计算逆运动学，各关节所需要的角度
            var jointAngles = CalculateInverseKinematics (targetPos);
            if ( jointAngles == null )
            {
                ScreenMessages. PostScreenMessage ("[CASNFP_Arm_SampleArm]Cannot calculate path to target", 5f);
                _isMoving = false;
                yield break;
            }
            //设置各关节目标角度
            for ( int i = 0 ; i < _servoModules.Length ; i++ )
            {
                SetJointTarget (i, jointAngles[i]);
            }
            //等待各关节运动到目标角度
            yield return StartCoroutine (WaitForJointsToReachTarget (jointAngles));
            //执行取样操作
            yield return StartCoroutine (PerformSampling());
            _isMoving=false;
            
        }

        private IEnumerator PerformSampling ()
        {
            _isSampling = true;
            Debug. Log ("startSampling");
            yield return new WaitForSeconds (3f);
        }
        private void OnDrawGizmos ()
        {
            if ( !startSample )
                return;
            if ( !HighLogic. LoadedSceneIsFlight )return;
            Gizmos. color = Color. green;
            Gizmos. DrawSphere (_targetPosition,0.1f);
            Gizmos. color = Color. cyan;
            Gizmos. DrawWireSphere (part. transform. position,3f);
            Gizmos. color = Color.yellow;
            for ( int i = 0 ; i < _servoModules.Length ; i++ )
            {
                Gizmos. DrawLine (_servoModules[i]. transform. position, _servoModules[i + 1]. transform. position);
            }
        }
        private IEnumerator WaitForJointsToReachTarget (float[] jointAngles)
        {
            bool allReached = false;
            float waitStartTime = Time. time;
            while ( !allReached && Time.time - waitStartTime< 30f ) //30秒超时；
            {
                allReached = true;
                float currentAngle = 0f;
                for ( int i = 0 ; i < _servoModules.Length ; i++ )
                {
                    
                    if ( _servoModules[i] is ModuleRoboticRotationServo rotationServo )
                    {
                       currentAngle = rotationServo.currentAngle;
                    }
                    else
                    {
                        if ( _servoModules[i] is ModuleRoboticServoHinge servoHinge )
                        {
                            currentAngle = servoHinge. currentAngle;
                        }
                    }
                    float angeleDiff = Mathf.Abs ( currentAngle - jointAngles[i] );
                    if ( angeleDiff > 0.1f )
                    {
                        allReached = false;
                        Debug. Log ($"[CASNFP_Arm_SampleArm](Joint {i})Not reached:{currentAngle:F1}` vs target{jointAngles[i]:F1}`");
                        break;
                    }
                    
                }
                yield return new WaitForSeconds ( 0.1f );
            }
            if ( !allReached )
            {
                ScreenMessages. PostScreenMessage ("[CASNFP_Arm_SampleArm]Arm movement timeOut!", 5f);
            }
            else
            {
                ScreenMessages. PostScreenMessage ("[CASNFP_Arm_SampleArm]Arm reached target position!", 3f);
            }
        }

        //设置关节角度
        private void SetJointTarget (int jointIndex, float targetAngle)
        {
            if ( jointIndex >= _servoModules. Length )
                return;
            var servo = _servoModules[jointIndex];
            if ( servo is ModuleRoboticRotationServo rotationServo )
            {
                rotationServo.targetAngle = targetAngle;
            }
            else
            {
                if ( servo is ModuleRoboticServoHinge servoHinge )
                {
                    servoHinge.targetAngle = targetAngle;
                }
            }
        }

        protected override float[] CalculateInverseKinematics (Vector3 targetPos)
        {
            if ( !IsPositionReachable(targetPos) )
            {
                return null;
            }
            try
            {
                //计算关节角度
                float[] angles = new float[_servoModules.Length];
                //基座node1旋转角度（Y轴旋转）
                Vector3 horizontalDiretion = toTarget;
                horizontalDiretion.y = 0;
                angles[0] = Vector3.SignedAngle(Vector3.forward,horizontalDiretion,Vector3.up);
                //大臂node2旋转角度（X轴旋转）
                float shoulderAngle = Mathf.Acos((Mathf.Pow(upperArmLenth,2)+Mathf.Pow(distanceToTarget,2)-Mathf.Pow(lowerArmLenth,2))/(2*upperArmLenth*distanceToTarget)) * Mathf. Rad2Deg;
                angles[1] = shoulderAngle;

                //小臂node3旋转角度（X轴旋转）
                float elbowAngle = Mathf. Acos (( Mathf. Pow (upperArmLenth, 2) + Mathf. Pow (lowerArmLenth, 2) - Mathf. Pow (distanceToTarget, 2) ) / ( 2 * upperArmLenth * distanceToTarget )) * Mathf. Rad2Deg;
                angles[2] = elbowAngle;
            
                //取样头保持水平
                angles[3] = -shoulderAngle-elbowAngle;
                return angles;
            }
            catch ( Exception ex)
            {
                Debug.LogError($"[CASNFP_Arm_SampleArm]IK Calculation failed:{ex.Message}");
                return null;
            }
            
        }
        Vector3 toTarget;
        float upperArmLenth;
        float lowerArmLenth;
        float distanceToTarget;
        private bool IsPositionReachable (Vector3 targetPos)
        {
            //距离检查
            float distance = Vector3.Distance (part.transform.position,targetPos);
            if ( _servoModules. Length < 3 )
            {
                Debug. LogError ("[CASNFP_Arm_SampleArm]Insufficient servo modules for IK calculation");
                return false;
            }
            //计算臂长是否大于距离

            //获取关节位置
            
            Vector3 basepos = _servoModules[0].MovingObject(). transform. position;
            Vector3 shoulderPos = _servoModules[1]. MovingObject (). transform. position;
            Vector3 elbowPos = _servoModules[2]. MovingObject (). transform. position;
            Vector3 wristPos = _servoModules[3]. MovingObject (). transform. position;
            Debug. Log ("basepos"+ basepos+ "shoulderPos"+shoulderPos+ "elbowPos"+ elbowPos+ "wristPos"+wristPos);

            //计算臂长
            upperArmLenth = Vector3. Distance (shoulderPos, basepos);
            lowerArmLenth = Vector3. Distance (elbowPos, shoulderPos);

            toTarget = targetPos - basepos;
            distanceToTarget = toTarget. magnitude;
            if ( distance > upperArmLenth + lowerArmLenth || distance < Math. Abs (upperArmLenth - lowerArmLenth) )
            {
                Debug. LogError ($"Target is to far:{distanceToTarget:F2} m> max{upperArmLenth + lowerArmLenth:F2}m");
                return false; 
            }
            else
            {
                Debug. Log ("Target can reach!");
                return true;
            }
        }

        
    }
}
