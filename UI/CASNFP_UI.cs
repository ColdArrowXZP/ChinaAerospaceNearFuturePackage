using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.UI
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor,false)]
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
            Debug.Log("项目预制按钮已生成在：" + LauncherButton.GetAnchor());
            LauncherButton.SetFalse();
        }
        public Rect rect = new Rect(0.5f, 0.5f, 200f, 100f);
        public MultiOptionDialog multi;
        PopupDialog popupDialog;
        [KSPEvent(guiActive = true,guiName ="",active =true)]
        protected override void OnTrue()
        {
            DialogGUIBox dialog = new DialogGUIBox(CASNFP_Globals.CASNFP_VERSION+"\n"+"测试UI功能",30f,30f);
            DialogGUIButton gUIButton1 = new DialogGUIButton("测试", onSelected, false);
            DialogGUIButton gUIButton2 = new DialogGUIButton("关闭", colseWindow);
            DialogGUIBase[] a = { dialog, gUIButton1,gUIButton2 };
            multi = new MultiOptionDialog("CASNFP_ControlPanel", "","中国航天包控制面板", HighLogic.UISkin, rect,a);
            
            popupDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),new Vector2(0.5f, 0.5f),multi,true,HighLogic.UISkin,false, "CASNFP_UI");
            Debug.Log("PopupDialog.SpawnPopupDialog 被调用");
            
        }

        private void onSelected()
        {

            ScreenMessages.PostScreenMessage("感谢支持", 2f, ScreenMessageStyle.UPPER_CENTER, Color.red);

        }

        private void colseWindow()
        {
            popupDialog.Dismiss();
            
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

