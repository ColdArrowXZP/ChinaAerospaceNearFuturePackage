using ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts;
using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using ChinaAeroSpaceNearFuturePackage. UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. UI
{
    [KSPAddon (KSPAddon. Startup. Flight, false)]
    public class MainControlPanel:AppLauncherBtn
    {

        private Rect rect = new Rect (0.5f, 0.5f, 300f, 200f);
        private MultiOptionDialog multi;
        private PopupDialog popupDialog;
        public List<ModuleCASNFP_RoboticArmPart> RoboticArms;
        public Vessel currentVessel;
        private ArmSelectUI armCtrlLogic;
        protected override void Awake ()
        {
            base.Awake ();
            RoboticArms = new List<ModuleCASNFP_RoboticArmPart> ();
            GameEvents.onFlightReady.Add (OnFlightReady);
        }

        private void OnFlightReady ()
        {
            RoboticArms. Clear ();
            if ( FlightGlobals. ActiveVessel != null )
            {
                currentVessel = FlightGlobals. ActiveVessel;
                RoboticArms = currentVessel. FindPartModulesImplementing<ModuleCASNFP_RoboticArmPart> ();
            }
            
        }

        protected override void OnTrue ()
        {
            DialogGUIBox dialogGUIBox = new DialogGUIBox ("版本号：V" + CASNFP_Globals. CASNFP_VERSION + "\n" + "欢迎使用中国航天包", 30f, 30f);
            DialogGUIButton armCtrlBtn = new DialogGUIButton ("启动机械臂自动控制程序", StartArmSelectUI, EnabledCondition:()=> { return RoboticArms. Count > 0; },false);
            DialogGUIButton closeBtn = new DialogGUIButton ("关闭CASNFP控制面板", () => { LauncherButton. SetFalse (); }, true);
            DialogGUIBase[] a = { dialogGUIBox, armCtrlBtn, closeBtn };
            multi = new MultiOptionDialog ("CASNFP_ControlPanel", "", "中国航天包控制面板", HighLogic. UISkin, rect, a);
            popupDialog = PopupDialog. SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), multi, false, HighLogic. UISkin, false, "CASNFP_UI");
        }
        private void StartArmSelectUI()
        {
            if (armCtrlLogic == null)
            {
                Debug.Log("Create ArmSelectUI第一步");

                // 确保游戏对象存在
                if (this == null)
                {
                    Debug.LogError("MainControlPanel实例已被销毁");
                    return;
                }

                if (this.gameObject == null)
                {
                    Debug.LogError("MainControlPanel的GameObject已被销毁");
                    return;
                }
                // 使用this.gameObject而不是gameObject
                if (this.gameObject.TryGetComponent<ArmSelectUI>(out armCtrlLogic))
                {
                    Debug.Log("Create ArmSelectUI第内一步");
                    armCtrlLogic.Start();
                }
                else
                {
                    Debug.Log("Create ArmSelectUI内二步");
                    armCtrlLogic = this.gameObject.AddComponent<ArmSelectUI>();
                    if (armCtrlLogic == null)
                    {
                        Debug.LogError("无法添加ArmSelectUI组件");
                        return;
                    }
                    armCtrlLogic.Start();
                }
            }
            else
            {
                Debug.Log("Create ArmSelectUI第二步");
                armCtrlLogic.Start();
            }

            Debug.Log("Create ArmSelectUI第三步");
            popupDialog?.Dismiss();
        }

        protected override void OnFalse ()
        {
            if ( popupDialog != null )
            popupDialog.Dismiss ();
            
        }
        protected override void OnDestroy ()
        {
            popupDialog?. Dismiss ();
            popupDialog. OnDismiss = null;
            armCtrlLogic = null;
            GameEvents.onFlightReady.Remove (OnFlightReady);
        }
    }
}
