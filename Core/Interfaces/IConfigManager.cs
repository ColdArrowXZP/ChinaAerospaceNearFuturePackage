using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Core. Interfaces
{
    public interface IConfigManager
    {
        ConfigNode GetConfigNode (string configName);
        void SaveConfig (ConfigNode node);
    }
}
