using UnityEngine;
using Expansions.Serenity;
using System;
using System.Collections.Generic;
namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class CASNFP_Arm : PartModule
    {
        [KSPField]
        public string servoHingeTransformNames = "";
        string[] servoHingeTransformName;
        Rigidbody[] rbs;
        ConfigurableJoint[] servoObjects;
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            servoHingeTransformName = servoHingeTransformNames.Split(',');
            servoObjects = new ConfigurableJoint[servoHingeTransformName.Length];
            rbs = new Rigidbody[servoHingeTransformName.Length];
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onFlightReady.Add(OnFlightReady);
                GameEvents.onPartDestroyed.Add(OnPartDestroyed);
            }
        }

        private void OnPartDestroyed(Part data)
        {
            if (data == part)
            {
                GameEvents.onFlightReady.Remove(OnFlightReady);
                GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            }
        }
        public override void OnUpdate() { }
        private void OnFlightReady()
        {
            
            for (int i = 0; i < servoHingeTransformName.Length; i++)
            {
                Debug.Log("开始for循环");
                GameObject objs = part.gameObject.GetChild(servoHingeTransformName[i]);
                Debug.Log(objs.name);
                servoObjects[i] = objs.GetComponent<ConfigurableJoint>();
                Debug.Log(i+ "     servoObjects      " + servoObjects[i].name);
                rbs[i] = objs.GetComponent<Rigidbody>();
                Debug.Log(i + "     rbs      " + servoObjects[i].name);
                if (i > 0) 
                {
                    servoObjects[i].connectedBody = rbs[i-1];
                }
            }
        }
        

    }


        //GameObject arm_UIPrefab;
        //GameObject arm_UIWindows;
        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "取样机械臂")]
        //public string title = "V1.0.0.0";
        //public override void OnStart(StartState state)
        //{
        //    string arm_UIPrefabName = SettingLoader.CASNFP_GlobalSettings.GetNode("Parts_Setting").GetNode("Arm_Setting").GetValue("Arm_UIPrefabName");
        //    arm_UIPrefab = SettingLoader.AppBundle.LoadAsset<GameObject>(arm_UIPrefabName);
        //}
        //[KSPEvent(guiName = "激活控制面板", active = true, guiActive = true, guiActiveEditor = true)]
        //public void ArmContolUI() 
        //{
        //    if (arm_UIWindows == null)
        //    {
        //        arm_UIWindows = Instantiate(arm_UIPrefab, part.transform);
        //        arm_UIWindows.SetActive(true);
        //    }
        //    else 
        //    {
        //        if (!arm_UIWindows.activeSelf)
        //        {
        //            arm_UIWindows.SetActive(true);
        //        }else arm_UIWindows.SetActive(false);
        //    }
        //}


}
    

