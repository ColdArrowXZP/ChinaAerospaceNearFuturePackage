using Expansions. Serenity;
using KSP. UI;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.UI
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class CASNFP_UI : AppLauncherBtn
    {
        private static CASNFP_UI instance;
        public static CASNFP_UI Instance { get => instance; }
        protected override void Awake ()
        {
            //在厂房或者飞行界面生成启动按钮。
            base. Awake ();
            //设置单例
            instance = this;
        }
        public Rect rect = new Rect(0.5f, 0.5f, 300f, 200f);
        public MultiOptionDialog multi;
        PopupDialog popupDialog;
        public Part[] CASNFP_RoboticArmPart;
        //[KSPEvent(guiActive = true,guiName ="",active =true)]
        protected override void OnTrue()
        {
            DialogGUIBox dialog = new DialogGUIBox("版本号：V"+CASNFP_Globals.CASNFP_VERSION+"\n"+"欢迎使用中国航天包",30f,30f);
            DialogGUIButton gUIButton1 = new DialogGUIButton("启动机械臂自动控制程序", StartArmAutoCtrl, false);
            DialogGUIButton gUIButton2 = new DialogGUIButton("关闭CASNFP控制面板", OnFalse, false);
            DialogGUIBase[] a = { dialog, gUIButton1,gUIButton2 };
            multi = new MultiOptionDialog("CASNFP_ControlPanel", "","中国航天包控制面板", HighLogic.UISkin, rect,a);
            
            popupDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),new Vector2(0.5f, 0.5f),multi,true,HighLogic.UISkin,false, "CASNFP_UI");
            Debug.Log("PopupDialog.SpawnPopupDialog 被调用");
            
        }
        protected virtual void StartArmAutoCtrl()
        {
            OnFalse ();
            // 启动机械臂自动控制程序
            StartingArmAutoCtrl ();
        }
        protected virtual void StartingArmAutoCtrl ()
        {
            Vessel vessel = FlightGlobals. ActiveVessel?.GetVessel ();
            if ( vessel == null )
            {
                MessageBox. Instance. ShowDialog ("错误", "当前不存在处于控制中的载具");
                return;
            }
            List<Part> CASNFP_RoboticArmPartList = new List<Part> ();
            List< ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm.ModuleCASNFP_RoboticArmPart> moduleCASNFP_RoboticArmParts = vessel. FindPartModulesImplementing< ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm.ModuleCASNFP_RoboticArmPart> ();
            if ( moduleCASNFP_RoboticArmParts == null || moduleCASNFP_RoboticArmParts. Count == 0 )
            {
                MessageBox. Instance. ShowDialog ("错误", "当前载具上没有CASNFP机械臂组件！");
                return;
            }
            else
            {
                foreach ( ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm. ModuleCASNFP_RoboticArmPart module in moduleCASNFP_RoboticArmParts )
                {
                    CASNFP_RoboticArmPartList. Add (module.part);
                }
                if (vessel.gameObject.GetComponent<ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm.CASNFP_RoboticArmAutoCtrl>() == null) 
                {
                    vessel.gameObject.AddComponent< ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm.CASNFP_RoboticArmAutoCtrl > ();
                }
                    
                vessel.gameObject.GetComponent< ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm.CASNFP_RoboticArmAutoCtrl > (). CASNFP_RoboticArmPart = CASNFP_RoboticArmPartList. ToArray ();
                vessel.gameObject.GetComponent<ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm.CASNFP_RoboticArmAutoCtrl>().Start ();


            }
        }

        protected override void OnFalse ()
        {
            popupDialog?.Dismiss ();
            if ( LauncherButton. toggleButton. CurrentState != UIRadioButton. State. False )
            {
                LauncherButton?.SetFalse ();
            }
        }
    }
}

