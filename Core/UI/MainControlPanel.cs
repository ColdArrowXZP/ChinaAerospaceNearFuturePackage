using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.Core. UI
{
    [KSPAddon (KSPAddon. Startup. Flight, false)]
    public class MainControlPanel:AppLauncherBtn
    {

        private Rect rect = new Rect (0.5f, 0.5f, 300f, 200f);
        private MultiOptionDialog multi;
        private PopupDialog popupDialog;
      
        protected override void OnTrue ()
        {
            base. OnTrue ();
            ClosePopupDialog ();
            DialogGUIBox dialogGUIBox = new DialogGUIBox ("版本号：V" + CASNFP_Globals. CASNFP_VERSION + "\n" + "欢迎使用中国航天包", 30f, 30f);
            DialogGUIButton armCtrlBtn = new DialogGUIButton ("启动机械臂自动控制程序", StartArmSelectUI);
            DialogGUIButton closeBtn = new DialogGUIButton ("关闭CASNFP控制面板", () => { LauncherButton. SetFalse (); }, true);
            DialogGUIBase[] a = { dialogGUIBox, armCtrlBtn, closeBtn };
            multi = new MultiOptionDialog ("CASNFP_ControlPanel", "", "中国航天包控制面板", HighLogic. UISkin, rect, a);
            popupDialog = PopupDialog. SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), multi, false, HighLogic. UISkin, false, "CASNFP_UI");
        }
        protected virtual void StartArmSelectUI() 
        {
            LauncherButton. SetFalse ();
        }

        protected override void OnFalse ()
        {
            base. OnFalse ();
            ClosePopupDialog ();

        }
        private void ClosePopupDialog ()
        {
            if ( popupDialog != null )
            {
                popupDialog. Dismiss ();
                popupDialog = null;
            }
        }
        protected override void OnDestroy ()
        {
            base. OnDestroy ();
            ClosePopupDialog ();
        }
        
    }
}
