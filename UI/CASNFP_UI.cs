using Expansions. Serenity;
using KSP. UI;
using System;
using UnityEngine;
using UnityEngine. EventSystems;
using UnityEngine. UI;
using UnityEngine. UIElements;
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
            CheckVesselIsContianCASNFP_RobArms ();
        }

        private void CheckVesselIsContianCASNFP_RobArms ()
        {
            Vessel vessel = FlightGlobals.ActiveVessel ?.GetVessel();
            if (vessel == null)
            {
                MessageBox.Instance.ShowDialog("错误", "当前不存在处于控制中的载具");
                return;
            }
            Debug.Log($"检查当前飞船{vessel.name}是否包含机械臂");
            if (vessel.FindPartModulesImplementing<BaseServo>().Count > 0)
            {
                MessageBox. Instance. ShowDialog ("提示", "当前飞船包含机械臂，开始自动控制程序",5f);
                BaseServo robArmServo = vessel.FindPartModulesImplementing<BaseServo> ()[0];
                if (robArmServo != null)
                {
                    Debug. Log ($"已检测到当前飞船存在{robArmServo.name}个CASNFP机械臂组件，开始自动控制程序");
                }
            }
            else
            {
                MessageBox. Instance. ShowDialog ("错误", "当前载具上没有CASNFP机械臂组件，把这里搞长一点，看看是不是会自动换行以及换行的效果是怎么样的，如果不行要考虑调整咯~！");
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

