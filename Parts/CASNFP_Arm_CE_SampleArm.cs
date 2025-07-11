using Expansions. Serenity;
using KSP. Localization;
using System;
using System. Collections;
using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts
{
    public class CASNFP_Arm_CE_SampleArm : CASNFP_RoboticArmBase
    {
        [KSPField]
        public string extendAngles = "";

        private float[] thisExtendAngles ;
        protected override float[] ExtendAngles 
        {
            get
            {
                return thisExtendAngles;
            } set
            {
                thisExtendAngles = value;
            }
        }

        private InverseKinematicsResult thisInverseKinematicsResult;
        protected override InverseKinematicsResult _InverseKinematicsResult
        {
            get
            {
                return thisInverseKinematicsResult;
            }
            set
            {
                thisInverseKinematicsResult = value;
            }
        }
        
        
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            // 初始化角度数组
            if ( string. IsNullOrEmpty (extendAngles) )
                extendAngles = "0,0,90,0";

            string[] angleStrings = extendAngles. Split (new[] { ',' }, StringSplitOptions. RemoveEmptyEntries);

            if ( _servoModules == null )
            {
                Debug. LogError ("[CASNFP_Arm_CE_SampleArm] _servoModules is null after FindServoModules!");
                return;
            }

            if ( angleStrings. Length != _servoModules. Length )
            {
                Debug. LogError ("[CASNFP_Arm_CE_SampleArm] extendAngles not correct, use default value.");
                thisExtendAngles = new float[_servoModules. Length];
            }
            else
            {
                thisExtendAngles = Array. ConvertAll (angleStrings, float. Parse);
            }
        }


        public override void OnUpdate ()
        {
            base. OnUpdate ();
            if ( !HighLogic. LoadedSceneIsFlight )
                return;
            if ( canSetTargetPos )
            {
                ScreenMessages. PostScreenMessage (Localizer. Format ("请左键选择取样地点", vessel. GetDisplayName ()), 1f, ScreenMessageStyle. UPPER_LEFT);
                //设置一个取样点
                if ( Input. GetMouseButtonDown (0) )
                {
                    Camera cam = FlightGlobals. fetch. mainCameraRef;
                    Ray ray = cam. ScreenPointToRay (Input. mousePosition);
                    RaycastHit hit;
                    Vector3 clickPoint = Vector3. zero;
                    if ( Physics. Raycast (ray, out hit) )
                    {
                        if ( hit. collider != null )
                        {
                            if ( hit. collider. gameObject. layer ==  15 )
                            {
                                clickPoint = hit. point;
                            }
                            else
                            {
                                ScreenMessages. PostScreenMessage (Localizer. Format ("选择取样地点为" + hit. collider. gameObject. name + "不正确，请选择地面取样点", vessel. GetDisplayName ()), 2f, ScreenMessageStyle. UPPER_RIGHT);
                                return;
                            }
                        }
                    }
                    if ( clickPoint == Vector3. zero )
                        return;//点击地面失败
                    thisInverseKinematicsResult = CalculateInverseKinematics(clickPoint);
                    if ( thisInverseKinematicsResult.success )
                    {
                        armAction. Invoke (this. part, RoboticArmState. Moving);
                    }
                    else
                    {
                        ScreenMessages. PostScreenMessage (Localizer. Format ("逆运动学计算失败，请检查取样点位置", vessel. GetDisplayName ()), 2f, ScreenMessageStyle. UPPER_RIGHT);
                    }
                }
            }
        }

        private  InverseKinematicsResult CalculateInverseKinematics(Vector3 targetPos)
        {
            //读取各伺服电机的角度限制
            List<Vector2> servoSoftLimits = new List<Vector2>();
            foreach (var servoModule in _servoModules)
            {
                if(servoModule is ModuleRoboticRotationServo servo)
                {
                    servoSoftLimits.Add(servoModule.GetSoftLimits("softMinMaxAngles"));
                    continue;
                }
                if(servoModule is ModuleRoboticServoHinge servoHinge)
                {
                    servoSoftLimits.Add(servoHinge.GetSoftLimits("softMinMaxAngles"));
                    continue;
                }
            }
            InverseKinematicsResult result = new InverseKinematicsResult
            {
                success = false,
                angles = new float[_servoModules. Length]
            };
            // 逆运动学计算逻辑
            // 这里需要实现逆运动学算法来计算每个伺服电机的角度
            //将目标位置转换为相对于机械臂旋转座_servoModules[0]的坐标并计算直线距离
            Vector3 localTargetPos = _servoModules[0].GetComponent<Rigidbody>().transform. InverseTransformPoint (targetPos);
            float distanceToTarget = Vector3. Distance (_servoModules[1].servoTransformPosition, localTargetPos);
            // 当前编辑的嫦娥取样机械臂只有大臂和小臂
            //计算各大臂的长度和机械臂总长
            float bigArmLength = Vector3. Distance (_servoModules[1]. servoTransformPosition, _servoModules[2]. servoTransformPosition);
            float smallArmLength = Vector3. Distance (_servoModules[2]. servoTransformPosition, _servoModules[3]. servoTransformPosition);
            //计算机械臂作用距离，用C平方=A平方+B平方-2ABcosC求出最大值和最小值
            Vector2 canRechDistence = new Vector2(
                Mathf. Abs (bigArmLength - smallArmLength),
                bigArmLength + smallArmLength
            );
            if ( canRechDistence.x>distanceToTarget || distanceToTarget > canRechDistence.y )
            {
                Debug. Log ("[CASNFP_Arm_CE_SampleArm]目标点超出机械臂最大长度，请重新选择取样点"); 
                return result;
            }
            //从这里开始计算逆运动学
            //计算旋转底座_servoModules[0]的角度，要求_servoModules[0].servoTransformPosition在XY平面上且Y轴指向目标点
            double cosTheta = localTargetPos.y/ Math.Sqrt(localTargetPos.x * localTargetPos.x + localTargetPos.y * localTargetPos.y);
            Debug.Log(cosTheta);
            double thetaRad = Math.Acos(cosTheta);
            Debug.Log($"[CASNFP_Arm_CE_SampleArm] thetaRad: {thetaRad}");
            // 转换为角度
            float thetaDeg = (float)(thetaRad * (180 / Math.PI));
            Debug.Log($"[CASNFP_Arm_CE_SampleArm] thetaDeg: {thetaDeg} float value!");

            if (localTargetPos.x <0)
            {
                thetaDeg = thetaDeg * -1;
            }

            // 计算大臂和小臂的角度
            float bigArmAngle = Mathf. Atan2 (localTargetPos. y, localTargetPos. x) * Mathf. Rad2Deg;
            float smallArmAngle = Mathf. Atan2 (localTargetPos. z, localTargetPos. x) * Mathf. Rad2Deg;
            // 检查角度是否在伺服电机的软限制范围内
            for (int i = 0; i < _servoModules.Length; i++)
            {
                if (i == 0)
                {
                    result.angles[i] = thetaDeg; // 基座角度保持不变
                }
                else if (i == 1)
                {
                    result.angles[i] = bigArmAngle;
                }
                else if (i == 2)
                {
                    result.angles[i] = smallArmAngle;
                }
                else
                {
                    result.angles[i] = thisExtendAngles[i]; // 其他伺服电机使用默认角度
                }
                // 检查角度是否在软限制范围内
                if (servoSoftLimits[i].x >= result.angles[i] || result.angles[i] >= servoSoftLimits[i].y)
                {
                    Debug.Log($"[CASNFP_Arm_CE_SampleArm] Servo {i} angle out of soft limits: {result.angles[i]} not in {servoSoftLimits[i]}");
                    return result; // 如果有一个伺服电机的角度超出限制，返回失败
                }
            }
            result.success = true;
            return result;
        }

    }
}
