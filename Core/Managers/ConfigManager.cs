using ChinaAeroSpaceNearFuturePackage. Core. Interfaces;
using System;
using System. IO;
using System. Reflection;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Core. Managers
{
    public class ConfigManager : IConfigManager
    {
        private static ConfigManager _instance;
        public static ConfigManager Instance => _instance ?? ( _instance = new ConfigManager () );

        private readonly string _settingFile;
        private ConfigNode _settingNode;
        private readonly string _settingsPath;
        private ConfigManager ()
        {
            _settingsPath = CASNFP_Globals.AssemblyPath + @"\Settings\";
            _settingFile = Path. Combine (_settingsPath,"CASNFPSettings.cfg");
            LoadConfigs ();
        }
        private void LoadConfigs ()
        {
            if ( !File. Exists (_settingFile) )
            {
                // 抛出异常来终止插件运行
                throw new FileNotFoundException ("主配置文件不存在，请确保项目文件完整。");
            }
            try
            {
                _settingNode = ConfigNode. Load (_settingFile);
            }
            catch ( System. Exception ex )
            {
                // 抛出异常来终止插件运行
                throw new Exception ($"加载配置文件失败: {ex. Message}");
            }
        }

        public ConfigNode GetConfigNode (string nodeName)
        {
            if ( !_settingNode. HasNode (nodeName) )
            {
                CASNFPLogger. Instance. LogError ("配置文件中不存在节点:" + nodeName);
                return null;
            }
            return _settingNode.GetNode (nodeName);
        }

        public void SaveConfig (ConfigNode node)
        {
            if ( _settingNode. HasNode (node. name) )
            {
                _settingNode.RemoveNode (node);
            }
            _settingNode.AddNode (node);
            node. Save (_settingFile);
        }
    }
}
