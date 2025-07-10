using Expansions. Serenity;
using System;
using UnityEngine;
using System. Collections;

namespace ChinaAeroSpaceNearFuturePackage. Parts
{
    public class CASNFP_Arm_CE_SampleArm : CASNFP_RoboticArm
    {
        
        public override float[] ExtendAngles => new float[4] { 0, 0,90, 0 };

        protected override Vector3 targetPosition => new Vector3(1,1,1);

        protected override void ArmStartWork()
        {
            Debug.Log("[CASNFP_Arm_CE_SampleArm]ArmStartWork");
        }

        protected override void ArmStopWork()
        {
            Debug.Log("[CASNFP_Arm_CE_SampleArm]ArmStopWork");
        }

        protected override InverseKinematicsResult CalculateInverseKinematics(Vector3 targetPos)
        {
            Debug.Log("[CASNFP_Arm_CE_SampleArm]CalculateInverseKinematics");
            return new InverseKinematicsResult
            {
                success = true,
                angles = new float[] { 0f, 0f, 0f }
            };
        }
    }
}
