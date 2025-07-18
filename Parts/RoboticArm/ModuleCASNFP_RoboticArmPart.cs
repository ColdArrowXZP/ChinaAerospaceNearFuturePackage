using System;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public class ModuleCASNFP_RoboticArmPart: PartModule
    {
        [KSPField(isPersistant = true)]
        public int thisPartBelongArmType = 0;// 机械臂组件类型枚举,0=基座,1=连接臂,2=工作臂,3=其他类型
        private ArmPartType armPartType;
        public ArmPartType ArmPartType
        {
            get => armPartType;
        }
        [KSPField(isPersistant = true)]
        public int thisPartBelongWorkType = 0; // 机械臂类型索引,0=取样（嫦娥）机械臂,1=吸盘式（天宫）机械臂,2=抓取式机械臂,3=摄像类机械臂，4=其他类型
        private ArmWorkType armType;
        public ArmWorkType ArmType
            {
            get => armType;
        }
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            switch ( thisPartBelongArmType )
            {
                case 0:
                    armPartType = ArmPartType. Base;
                    break;
                case 1:
                    armPartType = ArmPartType. link;
                    break;
                case 2:
                    armPartType = ArmPartType. work;
                    break;
                default:
                    throw new ArgumentOutOfRangeException (nameof (thisPartBelongArmType), "Invalid arm type index");
            }
            switch ( thisPartBelongWorkType )
            {
                case 0:
                    armType = ArmWorkType. ChangE;
                    break;
                case 1:
                    armType = ArmWorkType. TianGong;
                    break;
                case 2:

                    armType = ArmWorkType. Grabbing;
                    break;
                case 3:
                    armType = ArmWorkType. Camera;
                    break;
                default:
                    throw new ArgumentOutOfRangeException (nameof (thisPartBelongWorkType), "Invalid arm type index");
            }
        }

    }
}
