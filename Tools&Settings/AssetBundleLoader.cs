using System.IO;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage
{

    public static class AssetBundleLoader
    {
        
        public static AssetBundle LoadBundle(string bundleName)
        {
            string path = CASNFP_Globals.AssemblyPath + @"AssetBundles\"+bundleName+".ksp";
            if (File.Exists(path))
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                return bundle;
            }
            else
            {
                ScreenMessages.PostScreenMessage("资源包加载错误！", 10f, ScreenMessageStyle.LOWER_CENTER, Color.red);
                return default;
            }
        }
    }
}
