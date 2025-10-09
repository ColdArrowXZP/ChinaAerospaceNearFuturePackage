using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using Expansions. Serenity;
using System;
using System. Collections. Generic;
using System. Linq;
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
        public string basePosName = "basePos"; // 机械臂基座位置名称,用于机械臂基座位置定位
        private int armDefSpeed = 5; // 机械臂默认移动速度,单位度/秒
        [KSPField]
        public string baseTransform = "node1,10,Z,-180,180,0"; // 机械臂基座位置名称,用于机械臂基座位置定位
        [KSPField]
        public string effectTransform = "node4,10,X,-180,180,0"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        [KSPField(guiActive = true,)]
        public string LinksTransform = "node2,5,X,-107,107,0|node3,5,X,-180,180,0"; // 机械臂大臂段位置名称,用“,”隔开,用于机械臂大臂段位置定位
        private Transform workPos;
        private Transform basePos;
        private List<ArmJoint> joints = new List<ArmJoint> ();
        public ArmWorkType WorkType
        {
            get
            {
                return ( ArmWorkType )thisPartBelongWorkType;
            }
        }

        public ArmState armState = ArmState. Idle;

        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            GameEvents.onFlightReady. Add (OnFlightReady);
        }

        private void OnFlightReady ()
        {
            throw new NotImplementedException ();
        }
    }
}