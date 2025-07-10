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
            //将目标位置转换为相对于机械臂基座的坐标并计算直线距离
            Vector3 localTargetPos = transform. InverseTransformPoint (targetPos);
            float distanceToTarget = Vector3. Distance (_servoModules[1].servoTransformPosition, localTargetPos);
            // 当前编辑的嫦娥取样机械臂只有大臂和小臂
            //计算各大臂的长度和机械臂总长
            float bigArmLength = Vector3. Distance (_servoModules[1]. servoTransformPosition, _servoModules[2]. servoTransformPosition);
            float smallArmLength = Vector3. Distance (_servoModules[2]. servoTransformPosition, _servoModules[3]. servoTransformPosition);
            if ( distanceToTarget > bigArmLength + smallArmLength )
            {
                Debug. Log ("[CASNFP_Arm_CE_SampleArm]目标点超出机械臂最大长度，请重新选择取样点"); 
                return result;
            }
            result.success = true;
            result.angles = new float[] {90,90,90,90};
            return result;
        }

    }
}
