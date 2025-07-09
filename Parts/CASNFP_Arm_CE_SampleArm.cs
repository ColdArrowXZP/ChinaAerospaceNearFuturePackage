using Expansions. Serenity;
using System;
using UnityEngine;
using System. Collections;

namespace ChinaAeroSpaceNearFuturePackage. Parts
{
    public class CASNFP_Arm_CE_SampleArm: CASNFP_RoboticArm
    {
        //机械臂自由度数量
        protected override Vector3 targetPosition ()
        {
            Debug. Log ("[CASNFP_Arm_CE_SampleArm] target position is set!");
            return new Vector3 (0, 0, 0); // 示例位置，实际应根据需要设置
        }

        protected override float GetMaxReachDistance ()
        {
            return 2f;
        }

        protected override bool isCanReach (Vector3 targetPos)
        {
            throw new NotImplementedException ();
        }

        protected override void ArmStartWork ()
        {
            if ( !HighLogic.LoadedSceneIsFlight ) return;
            Material material = new Material (Shader. Find ("KSP/Particles/Additive"));
            material. SetColor ("_TintColor", Color. red);
            GameObject clickPointObj = GameObject. CreatePrimitive (PrimitiveType. Sphere);
            clickPointObj.GetComponent<Collider> (). enabled = false; // 禁用碰撞器
            clickPointObj. transform. position = base. _servoModules[1].transform.position;
            clickPointObj. transform. localScale = Vector3. one * 4f;
            clickPointObj. GetComponent<MeshRenderer> (). material = material;
            Vector3 targetPosition =  targetPosition();
            ScreenMessages. PostScreenMessage (armStartWork ? "机械臂开始工作，请注意安全！" : "机械臂停止工作！", 3f, ScreenMessageStyle. UPPER_CENTER);

        }

        protected override float[] CalculateInverseKinematics (Vector3 targetPos)
        {
            throw new NotImplementedException ();
        }
    }
}
