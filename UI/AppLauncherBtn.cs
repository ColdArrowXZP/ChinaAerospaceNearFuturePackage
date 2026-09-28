using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using Expansions;
using KSP. UI. Screens;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. UI
{
    public class AppLauncherBtn : MonoBehaviour
    {
        // 工具栏的启动按钮的背景图片
        private static Texture icon;
        /// <summary>
        /// 设置加载本项目UI资源包的AssetBundle,获得程序在工具栏的启动按钮
        /// </summary>
        private AssetBundle _AppBundle;

        private static ApplicationLauncherButton _LauncherButton;

        public static ApplicationLauncherButton LauncherButton
        {
            get => _LauncherButton;
        }

        /// <summary>
        /// 设置启动按钮生成时的方法，可以继承改写。
        /// </summary>
        protected virtual void Awake ()
        {
            CASNFPLogger. Instance. Log("AppLauncherBtn Awake");
            try
            {
                // 加载设置文件
                ConfigNode assetBundleNode = ConfigManager. Instance. GetConfigNode ("main_Setting");
                if ( assetBundleNode == null )
                {
                    CASNFPLogger. Instance. LogError ("无法获取主配置节点");
                    return;
                }
                string assetBundleName = assetBundleNode. GetValue ("AppBundleName");
                if ( string. IsNullOrEmpty (assetBundleName) )
                {
                    CASNFPLogger. Instance. LogError ("主配置节点中的AssetBundleName为空");
                    return;
                }
                _AppBundle = AssetBundleManager. Instance. LoadBundle (assetBundleName);
                if ( _AppBundle == null )
                {
                    CASNFPLogger. Instance. LogError ("无法加载资源包");
                    return;
                }

                ConfigNode node = ConfigManager. Instance. GetConfigNode ("UI_Setting");
                if ( node == null )
                {
                    CASNFPLogger. Instance. LogError ("无法获取UI配置节点");
                    return;
                }

                string _AppBtnPngName = node. GetValue ("AppBtnPngName");
                if ( string. IsNullOrEmpty (_AppBtnPngName) )
                {
                    CASNFPLogger. Instance. LogError ("AppBtnPngName为空");
                    return;
                }
                // 设置按钮背景图片
                if ( icon == null && _AppBundle != null )
                {
                    icon = _AppBundle. LoadAsset<Texture2D> (_AppBtnPngName);
                    if ( icon == null )
                    {
                        CASNFPLogger. Instance. LogError ("无法加载按钮图标");
                        return;
                    }
                }
                // 注册监听事件
                GameEvents. onGUIApplicationLauncherReady. Add (OnGUIApplicationLauncherReady);
                GameEvents. onGUIApplicationLauncherUnreadifying. Add (OnGUIApplicationLauncherUnreadifying);
            }
            catch ( System. Exception ex )
            {
                CASNFPLogger. Instance. LogError ($"插件初始化失败: {ex. Message}");
                this.enabled = false;
            }
        }

        protected virtual void OnDestroy ()
        {
            if ( _AppBundle != null )
                AssetBundleManager. Instance. UnloadBundle (_AppBundle);
            GameEvents. onGUIApplicationLauncherReady. Remove (OnGUIApplicationLauncherReady);
            GameEvents. onGUIApplicationLauncherUnreadifying. Remove (OnGUIApplicationLauncherUnreadifying);
        }

        /// <summary>
        /// 工具栏准备完成事件发生后执行
        /// </summary>
        protected void OnGUIApplicationLauncherReady ()
        {
            try
            {
                if ( ApplicationLauncher. Instance == null )
                {
                    CASNFPLogger. Instance. LogError ("ApplicationLauncher不可用");
                    return;
                }

                if ( LauncherButton == null )
                {
                    if ( icon == null )
                    {
                        CASNFPLogger. Instance. LogError ("按钮图标未加载");
                        return;
                    }

                    _LauncherButton = ApplicationLauncher. Instance. AddModApplication (
                        OnTrue, OnFalse, OnHover, OnHoverOut, OnEnable, OnDisable,
                        ApplicationLauncher. AppScenes. ALWAYS, icon);
                }
                OnReady ();
            }
            catch ( System. Exception ex )
            {
                CASNFPLogger. Instance. LogError ($"初始化工具栏按钮时发生错误: {ex. Message}");
                this.enabled = false;
            }
        }

        /// <summary>
        /// 工具栏不存在事件发生后执行
        /// </summary>
        protected void OnGUIApplicationLauncherUnreadifying (GameScenes scene)
        {
            // 删除启动按钮
            if ( ApplicationLauncher. Instance != null && _LauncherButton != null )
            {
                ApplicationLauncher. Instance. RemoveModApplication (_LauncherButton);
            }
            //按钮删除后立即执行该方法
            OnUnreadifying ();
        }

        /// <summary>
        /// 设置启动按钮回调虚方法，在UI中改写。
        /// </summary>
        protected virtual void OnDisable ()
        {
        }

        protected virtual void OnEnable ()
        {
        }

        protected virtual void OnFalse ()
        {
        }

        protected virtual void OnHover ()
        {
        }

        protected virtual void OnHoverOut ()
        {
        }

        protected virtual void OnReady ()
        {
        }

        protected virtual void OnTrue ()
        {
        }

        protected virtual void OnUnreadifying ()
        {
        }
    }
}