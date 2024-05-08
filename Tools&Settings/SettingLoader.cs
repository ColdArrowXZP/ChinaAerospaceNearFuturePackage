using CommNet.Network;
using System.IO;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage
{
    /// <summary>
    /// 启动时加载CASNFP项目配置文件，调用CANFP_GlobalSettings时返回配置文件。
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class SettingLoader:MonoBehaviour
    {
        private static ConfigNode _CASNFP_GlobalSettings;
        /// <summary>
        /// 返回CASNFP项目配置文件ConfigNode。
        /// </summary>
        public static ConfigNode CASNFP_GlobalSettings { get => _CASNFP_GlobalSettings; }
        /// <summary>
        /// 返回CASNFP项目资源包（包含UI的资源包，该包长期贮存游戏，所以不应太大）。
        /// </summary>
        public static AssetBundle UIBundle { get => _UIBundle; }

        private static AssetBundle _UIBundle;
        private string _AppBtnBundleName;
        private void Start()
        {
            string path = CASNFP_Globals.AssemblyPath + @"Settings\GlobalSettings.cfg";
            if (File.Exists(path))
            {
                _CASNFP_GlobalSettings = ConfigNode.Load(path);
            }
            else
            {
                ScreenMessages.PostScreenMessage("中国航天近未来包加载错误，请检查设置文件路径！", 10f, ScreenMessageStyle.LOWER_CENTER,Color.red);
            }
            if (CASNFP_GlobalSettings != default)
            {
                _AppBtnBundleName = CASNFP_GlobalSettings.GetNode("UI_Setting").GetValue("AppBtnBundleName");
                if (_UIBundle == default)
                {

                    _UIBundle = AssetBundleLoader.LoadBundle(_AppBtnBundleName);
                }
            }
            
        }
    }
}
