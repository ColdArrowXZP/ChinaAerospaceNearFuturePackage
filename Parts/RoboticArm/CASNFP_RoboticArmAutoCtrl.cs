using ChinaAeroSpaceNearFuturePackage.UI;
using System;
using System. Collections;
using System. Collections. Generic;
using UnityEngine;
using static VehiclePhysics. ProjectPatchAsset;
namespace ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm
{
    public class CASNFP_RoboticArmAutoCtrl : MonoBehaviour
    {
        
        /// <summary>
        /// 机械臂自动控制程序，先区分有几个机械臂，然后让玩家选择一个机械臂进行控制，判断机械臂类型，选择目标位置和目标姿态，控制机械臂到达目标位置和姿态，按计划开始工作。
        /// </summary>
        public void Start ()
        {
            MessageBox.Instance.ShowDialog("提示", "机械臂自动控制程序已启动");
            Part[] CASNFP_RoboticArmPart = CASNFP_UI.Instance.CASNFP_RoboticArmPart;
            Dictionary<ArmWorkType, Part[]> roboticArmIndex = CheckAndDistinguishTheArm (CASNFP_RoboticArmPart);
        }
        /// <summary>
        /// 创建机械臂选择窗口
        /// </summary>
        //IEnumerator CreatArmSelectionWindow (List<Part[]> roboticArmIndex)
        //{

        //    List<Part> parts1 = new List<Part> ();
        //    yield return new WaitUntil (() => parts1.Count > 0);
        //    currentArm = parts1.ToArray ();
        //}

        /// <summary>
        /// 检查机械臂部件并区分机械臂类型
        /// </summary>
        struct CheckAndDistinguishThePart
        {
            public ArmWorkType armWorkType;
            public Part part;
            public ArmPartType partType;
        }
        private Dictionary<ArmWorkType, Part[]> CheckAndDistinguishTheArm (Part[] cASNFP_RoboticArmPart)
        {
            Dictionary<ArmWorkType, List<Part>> roboticArmIndex = new Dictionary<ArmWorkType, List<Part>>();
            List<CheckAndDistinguishThePart> partsList = new List<CheckAndDistinguishThePart>();
            foreach (Part part in cASNFP_RoboticArmPart)
            {
                ModuleCASNFP_RoboticArmPart roboticArmModule = part.Modules.GetModule<ModuleCASNFP_RoboticArmPart>();
                CheckAndDistinguishThePart thePart = new CheckAndDistinguishThePart
                {
                    part = part,
                    armWorkType = roboticArmModule.ArmType,
                    partType = roboticArmModule.ArmPartType
                };
                partsList.Add(thePart);
            }
            foreach (var item in partsList)
            {
                switch ( item.armWorkType )
                {
                    case ArmWorkType.ChangE:
                        if (!roboticArmIndex.ContainsKey(ArmWorkType.ChangE))
                        {
                            roboticArmIndex[ArmWorkType.ChangE] = new List<Part>();
                        }
                        roboticArmIndex[ArmWorkType.ChangE].Add(item.part);
                        break;
                }
            }
            return roboticArmIndex;
        }
    }
   
}