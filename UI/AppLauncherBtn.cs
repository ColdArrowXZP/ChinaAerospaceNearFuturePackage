using UnityEngine;
using KSP.UI.Screens;

namespace ChinaAeroSpaceNearFuturePackage.UI
{
    
    public class AppLauncherBtn : MonoBehaviour
    {
        /// <summary>
        /// 工具栏的启动按钮的背景图片
        /// </summary>
        private static Texture icon;
        /// <summary>
        /// 设置加载本项目UI资源包
        /// </summary>
        /// <summary>
        /// 获得程序在工具栏的启动按钮
        /// </summary>
        private  ApplicationLauncherButton _LauncherButton;
        public  ApplicationLauncherButton LauncherButton { get => _LauncherButton; }
        /// <summary>
        /// 设置启动按钮生成时的方法，可以继承改写。
        /// </summary>
        protected virtual void Awake()
        {
            ConfigNode node = SettingLoader.CASNFP_GlobalSettings;
            string _AppBtnPngName = node.GetNode("UI_Setting").GetValue("AppBtnPngName");
            //设置按钮背景图片
            if (icon == null && SettingLoader.AppBundle != null)
            {
                icon = SettingLoader.AppBundle.LoadAsset<Texture2D>(_AppBtnPngName);
            }
            // 注册监听事件
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(OnGUIApplicationLauncherUnreadifying);
        }
        protected virtual void OnDestroy()
        {
            // 注销监听事件
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(OnGUIApplicationLauncherUnreadifying);
        }
        /// <summary>
        /// 工具栏准备完成事件发生后执行
        /// </summary>
        protected void OnGUIApplicationLauncherReady()
        {
            // 生成启动按钮
            if (ApplicationLauncher.Instance != null)
            {
                _LauncherButton = ApplicationLauncher.Instance.AddModApplication(OnTrue, OnFalse, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
                //设置工具栏按钮之间互斥。
                //ApplicationLauncher.Instance.EnableMutuallyExclusive(_LauncherButton);
            }
            //按钮生成后立即执行该方法
            OnReady();
        }
        /// <summary>
        /// 工具栏不存在事件发生后执行
        /// </summary>
        protected void OnGUIApplicationLauncherUnreadifying(GameScenes scene)
        {
            // 删除启动按钮
            if (ApplicationLauncher.Instance != null && _LauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_LauncherButton);
            }
            //按钮删除后立即执行该方法
            OnUnreadifying();
        }
        /// <summary>
        /// 设置启动按钮回调虚方法，在UI中改写。
        /// </summary>
        protected virtual void OnDisable() { }
        protected virtual void OnEnable() { }
        protected virtual void OnFalse() { }
        protected virtual void OnHover() { }
        protected virtual void OnHoverOut() { }
        protected virtual void OnReady() { }
        protected virtual void OnTrue() {}
        protected virtual void OnUnreadifying() { }
    }
}
