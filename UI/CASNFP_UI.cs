using ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm;
using Expansions. Serenity;
using KSP. UI;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.UI
{
    [KSPAddon (KSPAddon. Startup. Flight,false)]
    public class CASNFP_UI : AppLauncherBtn
    {
        private CASNFP_UI (){}
        private static CASNFP_UI instance;
        public static CASNFP_UI Instance { get => instance; }
        private CASNFP_SetRocAutoCtrl _armAutoCtrl;
        public CASNFP_SetRocAutoCtrl ArmAutoCtrl
        {
            get { return _armAutoCtrl; }
        }
        protected override void Awake ()
        {
            //设置单例
            if ( instance == null )
            {
                instance = this;
            }
            //在飞行界面生成启动按钮。
            base. Awake ();
            
            
        }
        public Rect rect = new Rect(0.5f, 0.5f, 300f, 200f);
        public MultiOptionDialog multi;
        PopupDialog popupDialog;
        public ModuleCASNFP_RoboticArmPart[] CASNFP_RoboticArmPart;
        //[KSPEvent(guiActive = true,guiName ="",active =true)]
        protected override void OnTrue()
        {
            DialogGUIBox dialog = new DialogGUIBox("版本号：V"+CASNFP_Globals.CASNFP_VERSION+"\n"+"欢迎使用中国航天包",30f,30f);
            DialogGUIButton gUIButton1 = new DialogGUIButton ("启动机械臂自动控制程序", StartArmAutoCtrl, false);
            DialogGUIButton gUIButton2 = new DialogGUIButton("关闭CASNFP控制面板", OnFalse, false);
            DialogGUIBase[] a = { dialog, gUIButton1,gUIButton2 };
            multi = new MultiOptionDialog("CASNFP_ControlPanel", "","中国航天包控制面板", HighLogic.UISkin, rect,a);
            
            popupDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),new Vector2(0.5f, 0.5f),multi,true,HighLogic.UISkin,false, "CASNFP_UI");
        }
        protected virtual void StartArmAutoCtrl()
        {
            OnFalse ();
            if ( CASNFP_RoboticArmPart == null )
            {
                SetArmJointInfos ();
                StartAutoCtrl ();
            }
            else
                StartAutoCtrl ();
            if ( _armAutoCtrl == null )
            {
                Debug. LogError ("错误："+"机械臂识别过程中发生错误，自动控制程序终止");
            }

        }
        protected virtual void SetArmJointInfos ()
        {
            Vessel vessel = FlightGlobals. ActiveVessel?.GetVessel ();
            if ( vessel == null )
            {
                Debug. Log ("错误:" + "当前不存在处于控制中的载具");
                return;
            }
            //获取当前载具上的所有机械臂组件，将index为连续的机械臂组件放入机械臂数组中，如果中间有中断则划分为新的机械臂数组；检查同一机械臂数组是否符合至少一个底座、一个连接臂和一个工作臂的条件，符合条件则添加到机械臂列表中，不符合则忽略该机械臂数组并给出提示信息。
            List<ModuleCASNFP_RoboticArmPart> armParts = new List<ModuleCASNFP_RoboticArmPart> ();
            foreach ( Part part in vessel. Parts )
            {
                ModuleCASNFP_RoboticArmPart item = part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> ();
                if ( item != null )
                {
                    armParts. Add (item);
                }
            }
            if ( armParts. Count == 0 )
            {
                Debug. Log ("错误:" + "当前载具上不存在机械臂组件");
                return;
            }
            CASNFP_RoboticArmPart = armParts. ToArray ();
        }
        //启动机械臂自动控制的UI控制窗口
        private void StartAutoCtrl ()
        {
            if ( CASNFP_RoboticArmPart. Length != 0 )
            {
                if ( !gameObject. TryGetComponent<CASNFP_SetRocAutoCtrl> (out _armAutoCtrl) )
                {
                    _armAutoCtrl = gameObject. AddComponent<CASNFP_SetRocAutoCtrl> ();
                }
                else
                    _armAutoCtrl. Start ();
            }else Debug. LogError ("错误："+"机械臂识别过程中发生错误，自动控制程序终止");
        }

        protected override void OnDestroy ()
        {
            base. OnDestroy ();
            if(popupDialog != null ) {PopupDialog.Destroy (popupDialog);}
            if ( ArmAutoCtrl != null )
            {
                Destroy (_armAutoCtrl);
            }
            if ( instance != null )
            {
                instance = null;
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

