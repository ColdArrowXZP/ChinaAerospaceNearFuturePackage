using ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts;
using System;
using System.Collections.Generic;
using System. Xml. Linq;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.Core. UI
{
    [KSPAddon (KSPAddon. Startup.FlightAndEditor, false)]
    public class MainControlPanel:AppLauncherBtn
    {
        private PopupDialog popupDialog;
        [KSPField]
        public string currentArmIndex = "1";
        protected override void OnTrue ()
        {
            base. OnTrue ();
            ClosePopupDialog ();
            ShowMainDialog ();
        }
        public void ShowMainDialog ()
        {
            UIStyle centeredStyle = new UIStyle (HighLogic. UISkin. label) 
            {
                alignment = TextAnchor.LowerRight,
                stretchWidth = true,
                fontSize = 12
            };
            DialogGUIBase[] elements = new DialogGUIBase[]
           {
                new DialogGUIButton ("启动机械臂控制程序", () => { LauncherButton. SetFalse ();StartArmSelectUI (); }, true), 
                new DialogGUIButton ("关闭控制面板", () => { LauncherButton. SetFalse (); }, true),
                new DialogGUISpace (5f),
                new DialogGUILabel("V:"+CASNFP_Globals. CASNFP_VERSION,centeredStyle,true,false),
           };

            // 构建一个垂直布局，它会自动计算子元素的大小
            DialogGUIVerticalLayout mainLayout = new DialogGUIVerticalLayout (
                true,
                true,
                5f,
                new RectOffset (10, 10, 10, 10),
                TextAnchor.MiddleCenter,
                elements
            );
            // 创建一个 DialogGUIVerticalLayout，它会自动计算子元素的大小:
            // public DialogGUIVerticalLayout (bool sw, bool sh, float sp, RectOffset pad, TextAnchor achr, params DialogGUIBase[] list);
            //sh = true‌: 告诉布局系统，这个垂直布局的高度应该由其子元素的总高度决定。这是实现“根据内容动态调整高度”的核心。
            //‌sw = true‌: 通常也建议设置为 true，以便宽度也能适应内容或父容器，避免内容被裁剪或留下过多空白。
            //‌sp (spacing)‌: 子元素之间的间距。
            //‌pad (padding)‌: 布局内部的内边距。
            //‌achr (anchor)‌: 子元素的对齐方式。
            // 创建 MultiOptionDialog，它会根据 layout 自动调整大小
            MultiOptionDialog multi = new MultiOptionDialog (
                "CASNFP_ControlPanel",
                "",
                "中国航天包控制面板",
                UISkinManager.GetSkin("KSP window 7"),
                mainLayout
            );
            //public static PopupDialog SpawnPopupDialog (MultiOptionDialog dialog, bool persistAcrossScenes, UISkinDef skin, bool isModal = true, string titleExtra = "");
            // 1. MultiOptionDialog dialog
            //‌含义‌：对话框的内容定义对象。
            //2. bool persistAcrossScenes
            //‌含义‌：是否跨场景持久化。
            //‌作用‌：
            //‌true‌：当玩家切换场景（例如从太空中心切换到飞行场景，或加载存档）时，该弹窗不会被自动销毁，而是会尝试保留并继续显示。这通常用于全局通知或需要长期存在的提示。
            //‌false‌：当场景切换时，弹窗会被自动销毁。这是大多数临时性交互对话框（如确认菜单、部件配置窗口）的标准设置，以防止内存泄漏或在错误的场景中显示无关UI。
            //3. UISkinDef skin
            //‌含义‌：UI 皮肤定义。
            //‌作用‌：决定弹窗的视觉风格，包括背景纹理、字体、按钮样式、颜色等。
            //‌常用值‌：
            //HighLogic. UISkin：最常用的默认皮肤，适用于大多数主菜单和飞行中的标准 UI。
            //也可以传入自定义的 UISkinDef 对象来实现模组特有的视觉风格。
            //4. bool isModal = true
            //‌含义‌：是否为模态对话框。
            //‌作用‌：
            //‌true (模态)‌：弹窗显示时，会阻止用户与弹窗背后的其他 UI 元素或游戏世界进行交互。用户必须先关闭或处理当前弹窗才能进行其他操作。通常用于确认警告、关键设置或必须引起注意的信息。
            //‌false (非模态)‌：弹窗显示时，用户仍然可以点击和操作背景中的其他 UI 或游戏对象。通常用于提示信息、日志窗口或辅助工具面板。
            //5. string titleExtra = ""
            //‌含义‌：额外的标题字符串。
            popupDialog = PopupDialog. SpawnPopupDialog (multi, false, UISkinManager. GetSkin ("KSP window 7"), false, "");
        }
        protected virtual void StartArmSelectUI() 
        {
            Debug. Log ("启动机械臂控制程序逻辑");
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
