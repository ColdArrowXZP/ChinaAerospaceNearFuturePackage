using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public class ModuleCASNFP_RoboticArmPart : PartModule
    {
        [KSPField]
        public int thisPartBelongWorkType = 0; // 机械臂类型索引,0=取样（嫦娥）机械臂,1=吸盘式（天宫）机械臂,2=抓取式机械臂,3=摄像类机械臂，4=其他类型
        [KSPField]
        public string workPosName = "workDrill"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        [KSPField]
        public string basePosName = "node1"; // 机械臂基座位置名称,用于机械臂基座位置定位
        [KSPField]
        public string baseJointInfo = "node1,10,Z,-180,180,0"; // 机械臂基座位置名称,用于机械臂基座位置定位
        [KSPField]
        public string effectJointInfo = "node4,10,X,-180,180,0"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        [KSPField]
        public string linkJointsInfo = "node2,10,X,-107,107,0|node3,10,X,-180,180,0"; // 机械臂大臂段位置名称,用“,”隔开,用于机械臂大臂段位置定位
        public Transform workPos;
        public Transform basePos;
        public ArmJoint[] joints;
        public ArmState ArmCurrentState{get;set;}
        public ArmWorkType WorkType
        {
            get
            {
                return ( ArmWorkType )thisPartBelongWorkType;
            }
        }
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            if ( HighLogic. LoadedScene != GameScenes. FLIGHT )
                return;
            InstializeJoints ();
        }
        private void InstializeJoints() 
        {   //初始化关节
            workPos = this. part. FindModelTransform (workPosName);
            basePos = this.part. FindModelTransform ( basePosName);
            ArmJoint[] linkJoints = ArmHelper.SetJointWithString(linkJointsInfo,this.part).ToArray();
            joints = new ArmJoint[linkJoints.Length + 2];
            joints[0] = ArmHelper.SetJointWithString(baseJointInfo, this.part)[0];
            joints[joints.Length - 1] = ArmHelper.SetJointWithString(effectJointInfo, this. part)[0];
            for (int i = 0; i < linkJoints.Length; i++)
            {
                joints[i + 1] = linkJoints[i];
            }
            foreach (ArmJoint joint in joints)
            {
                joint.Init(); 
            }
            ArmCurrentState = ArmState.Idle;
        }

    }
}