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
        private void StartArmSelectUI ()
        {
            
            if ( armCtrlLogic == null )
            {
                armCtrlLogic = this.gameObject. AddComponent<ArmSelectUI> ();
            }
            else
            {
                armCtrlLogic. Start ();
            }
            popupDialog?.Dismiss ();
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
