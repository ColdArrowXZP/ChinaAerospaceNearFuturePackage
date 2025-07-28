using System.IO;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage
{
    /// <summary>
    /// 启动时加载CASNFP项目配置文件，调用CANFP_GlobalSettings时返回配置文件。
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
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
        public static AssetBundle AppBundle { get => _AppBundle; }

        private static AssetBundle _AppBundle;
        private void Start()
        {
            string path = CASNFP_Globals.AssemblyPath + @"Settings\GlobalSettings.cfg";
            if (File.Exists(path))
            {
                _CASNFP_GlobalSettings = ConfigNode.Load(path);
            }
            else
            {
                Debug.Log("ChinaAeroSpaceNearFuturePackage:Settings file is not found,delete AssemblyFile");
            }
            if (CASNFP_GlobalSettings != default)
            {
                string _AppBundleName = CASNFP_GlobalSettings.GetNode("Globals_Setting").GetValue("AppBundleName");
                if (_AppBundle == default)
                {

                    _AppBundle = AssetBundleLoader.LoadBundle(_AppBundleName);
                }
            }
        }
    }
}
