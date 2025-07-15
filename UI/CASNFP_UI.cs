using KSP. UI;
using System;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.UI
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class CASNFP_UI : AppLauncherBtn
    {
        private static CASNFP_UI instance;
        public static CASNFP_UI Instance { get => instance; }
        protected override void Awake()
        {
            //在厂房或者飞行界面生成启动按钮。
            base.Awake();
            //设置单例
            instance = this;

        }
        protected override void OnReady()
        {
            
        }
        public Rect rect = new Rect(0.5f, 0.5f, 300f, 200f);
        public MultiOptionDialog multi;
        PopupDialog popupDialog;
        //[KSPEvent(guiActive = true,guiName ="",active =true)]
        protected override void OnTrue()
        {
            DialogGUIBox dialog = new DialogGUIBox("版本号：V"+CASNFP_Globals.CASNFP_VERSION+"\n"+"欢迎使用中国航天包",30f,30f);
            DialogGUIButton gUIButton1 = new DialogGUIButton("测试", OnSelected, false);
            DialogGUIButton gUIButton2 = new DialogGUIButton("关闭", OnClosed, false);
            DialogGUIBase[] a = { dialog, gUIButton1,gUIButton2 };
            multi = new MultiOptionDialog("CASNFP_ControlPanel", "","中国航天包控制面板", HighLogic.UISkin, rect,a);
            
            popupDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),new Vector2(0.5f, 0.5f),multi,true,HighLogic.UISkin,false, "CASNFP_UI");
            Debug.Log("PopupDialog.SpawnPopupDialog 被调用");
            
        }

        private void OnClosed()
        {
            ScreenMessages.PostScreenMessage("测试关闭", 2f, ScreenMessageStyle.UPPER_CENTER, Color.red);
        }

        private void OnSelected()
        {

            ScreenMessages.PostScreenMessage("感谢支持", 2f, ScreenMessageStyle.UPPER_CENTER, Color.red);

        }

        protected override void OnFalse()
        {
            if (popupDialog != null)
            {
                popupDialog.Dismiss();
            }
        }
       
    }
}

