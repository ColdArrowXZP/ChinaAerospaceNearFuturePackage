using System;
using UnityEngine;
using UnityEngine.UI;
using Expansions;
using Expansions.Serenity;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using CommNet.Network;
namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class CASNFP_Arm : PartModule
    {

        GameObject arm_UIPrefab;
        GameObject arm_UIWindows;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CASNFP机械臂")]
        public string title = "V1.0.0.0";
        public override void OnStart(StartState state)
        {
            string arm_UIPrefabName = SettingLoader.CASNFP_GlobalSettings.GetNode("Parts_Setting").GetNode("Arm_Setting").GetValue("Arm_UIPrefabName");
            arm_UIPrefab = SettingLoader.AppBundle.LoadAsset<GameObject>(arm_UIPrefabName);
        }
        [KSPEvent(guiName = "激活控制面板", active = true, guiActive = true, guiActiveEditor = true)]
        public void ArmContolUI() 
        {
            if (arm_UIWindows == null)
            {
                arm_UIWindows = Instantiate(arm_UIPrefab, part.transform);
                arm_UIWindows.SetActive(true);
            }
            else 
            {
                if (!arm_UIWindows.activeSelf)
                {
                    arm_UIWindows.SetActive(true);
                }else arm_UIWindows.SetActive(false);
            }
        }
        
    }
}
    

