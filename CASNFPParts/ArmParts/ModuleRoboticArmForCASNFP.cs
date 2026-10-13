using ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts;
using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using System;
using System.Collections;
using System. Reflection;
using UnityEngine;

/// <summary>
/// 动态机械臂模块：挂在机械臂基座Part上，运行时自动生成独立物理的大臂/小臂子Part
/// </summary>

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    
    public class ModuleRoboticArmForCASNFP : BaseManipulator
    {
        private ArmType armType;
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            CheckArmType (part. partInfo. partConfig);
        }

        private void CheckArmType (ConfigNode partConfig)
        {
            int index = int.Parse(partConfig. GetNode ("MODULE", "name", this. moduleName).GetValue("armType")) ;
            switch( index )
                {
                case 0:
                    armType = ArmType.Sampling;
                    break;
                case 1:
                    armType = ArmType.Grasping;
                    break;
                case 2:
                    armType = ArmType.Crawling;
                    break;
                case 3:
                    armType = ArmType.Crawling;
                    break;
                default:
                    armType = ArmType.Sampling;
                    break;
            }
        }

        protected override void RotateJointInFlight (int index)
        {
            if ( index < 0 || index >= jointsSaved. Length )
            {
                CASNFPLogger. Instance. LogError ($"无效的关节索引: {index}");
                return;
            }

            SaveJointInfo joint = jointsSaved[index];
            if ( joint. jointIsLocked )
            {
                ScreenMessages. PostScreenMessage ("机械臂已锁定，无法操作", 3f, ScreenMessageStyle. LOWER_CENTER);
                return;
            }
            if ( joint. jointTransformName == null )
            {
                CASNFPLogger. Instance. LogError ($"关节 {index} 的 Transform 未设置");
                return;
            }
            // 获取当前角度
            float currentAngle = part. gameObject. GetChild (joint. jointTransformName). transform. localRotation. eulerAngles[joint. axis == Vector3. right || joint. axis == Vector3. left ? 0 :
                ( joint. axis == Vector3. up || joint. axis == Vector3. down ? 1 : 2 )];

            // 平滑插值到目标角度
            float targetAngle = Mathf. Clamp (joint. targetAngle, joint. minMaxRotationAngle. x, joint. minMaxRotationAngle. y);
            float newAngle = Mathf. Lerp (currentAngle, targetAngle, Time. deltaTime * 2f); // 2f是旋转速度，可以调整

            // 限制角度在允许范围内
            newAngle = Mathf. Clamp (newAngle, joint. minMaxRotationAngle. x, joint. minMaxRotationAngle. y);

            // 计算旋转
            Quaternion rotation = Quaternion. AngleAxis (newAngle, joint. axis);

            // 应用旋转
            part. gameObject. GetChild (joint. jointTransformName). transform. localRotation = rotation;
            jointsSaved[index]. startAngle = newAngle;
        }
    }
       
}




