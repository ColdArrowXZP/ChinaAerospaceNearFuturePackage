using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Core. Interfaces
{
    public interface IAssetBundleLoader
    {
        AssetBundle LoadBundle (string bundleName);
        void UnloadBundle (AssetBundle bundle);
    }
}
