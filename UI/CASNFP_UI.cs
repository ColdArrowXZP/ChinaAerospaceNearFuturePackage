using Expansions. Serenity;
using KSP. UI;
using System;
using System.Collections.Generic;
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
        public Part[] CASNFP_RoboticArmPart;
        private void CheckVesselIsContianCASNFP_RobArms ()
        {
            Vessel vessel = FlightGlobals.ActiveVessel ?.GetVessel();
            if (vessel == null)
            {
                MessageBox.Instance.ShowDialog("错误", "当前不存在处于控制中的载具");
                return;
            }
            Debug.Log($"检查当前飞船{vessel.name}是否包含CASNFP机械臂组件");
            List<Part> CASNFP_RoboticArmPartList  = new List<Part>();
            List<BaseServo> baseServos = vessel.FindPartModulesImplementing<BaseServo>();
            if (baseServos == null || baseServos.Count == 0)
            {
                MessageBox.Instance.ShowDialog("错误", "当前载具上没有机械关节组件！");
                return;
            }
            else 
            {
                foreach (BaseServo servo in baseServos)
                {
                    //查找含有CASNFP机械臂的部件
                    if (servo.part.name.IndexOf("CASNFP", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        CASNFP_RoboticArmPartList.Add(servo.part);
                    }
                }

            }
            if (CASNFP_RoboticArmPartList.Count == 0)
            {
                MessageBox.Instance.ShowDialog("错误", "当前载具上没有CASNFP机械臂组件，把这里搞长一点，看看是不是会自动换行以及换行的效果是怎么样的，如果不行要考虑调整咯~！");
                return;
            }
            else 
            {
                CASNFP_RoboticArmPart = CASNFP_RoboticArmPartList.ToArray();
                MessageBox.Instance.ShowDialog("成功", $"当前载具上包含{CASNFP_RoboticArmPart.Length}个CASNFP机械臂组件，正在启动机械臂自动控制程序");
                foreach (var item in CASNFP_RoboticArmPart)
                {
                    Debug.Log($"含有CASNFP机械臂的组件名称为{item.partName},在飞船上的组件列表中的序列号为{vessel.parts.IndexOf(item)}");
                }
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

