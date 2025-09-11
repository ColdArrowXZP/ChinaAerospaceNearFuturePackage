using Expansions. Serenity;
using System;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public class ModuleCASNFP_RoboticArmPart: PartModule
    {
        [KSPField(isPersistant = true)]
        public string jointName = "ArmJoint"; // 机械臂关节名称,用于机械臂关节连接
        [KSPField(isPersistant = true)]
        public int thisPartBelongArmType = 0;// 机械臂组件类型枚举,0=基座,1=连接臂,2=工作臂,3=其他类型
        [KSPField(isPersistant = true)]
        public int thisPartBelongWorkType = 0; // 机械臂类型索引,0=取样（嫦娥）机械臂,1=吸盘式（天宫）机械臂,2=抓取式机械臂,3=摄像类机械臂，4=其他类型
        [KSPField (isPersistant = true)]
        public float armPartLength = 0f; // 机械臂组件长度,用于计算机械臂总长
        [KSPField (isPersistant = true)]
        public string workPosName = "workPos"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        private ArmPartJointInfo armPartJointInfo;
        public ArmPartJointInfo ArmPartJointInfo
        {
            get => armPartJointInfo;
        }
        public Transform workPosTransform
        {
            get
            {
                if ( workPosName != "" && part.FindModelTransform (workPosName) != null )
                    return part. FindModelTransform (workPosName);
                else
                    Debug.Log("workPosName is empty or not found, using part transform instead.");
                return part. transform;
            }
        }
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            armPartJointInfo = new ArmPartJointInfo ();
            switch ( thisPartBelongArmType )
            {
                case 0:
                    armPartJointInfo. partType = ArmPartType. Base;
                    break;
                case 1:
                    armPartJointInfo. partType = ArmPartType. link;
                    break;
                case 2:
                    armPartJointInfo. partType = ArmPartType. work;
                    break;
                default:
                    throw new ArgumentOutOfRangeException (nameof (thisPartBelongArmType), "Invalid arm type index");
            }
            switch ( thisPartBelongWorkType )
            {
                case 0:
                    armPartJointInfo. armWorkType = ArmWorkType. Sample_ChangE;
                    break;
                case 1:
                    armPartJointInfo. armWorkType = ArmWorkType. Walk_TianGong;
                    break;
                case 2:
                    armPartJointInfo. armWorkType = ArmWorkType. Grabbing;
                    break;
                case 3:
                    armPartJointInfo. armWorkType = ArmWorkType. Camera;
                    break;
                default:
                    throw new ArgumentOutOfRangeException (nameof (thisPartBelongWorkType), "Invalid arm type index");
            }
        }
    }
}
