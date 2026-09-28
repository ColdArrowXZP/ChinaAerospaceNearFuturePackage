using System. Collections. Generic;
using System. IO;
using UnityEngine;
using ChinaAeroSpaceNearFuturePackage. Core. Interfaces;
using Expansions;
using System;

namespace ChinaAeroSpaceNearFuturePackage. Core. Managers
{
    public class AssetBundleManager : IAssetBundleLoader
    {
        private static AssetBundleManager _instance;
        public static AssetBundleManager Instance => _instance ?? ( _instance = new AssetBundleManager ()
        );

        private readonly  Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle> ();

        public AssetBundle LoadBundle (string bundleName)
        {
            if ( string. IsNullOrEmpty (bundleName) )
            {
                CASNFPLogger. Instance. LogError ("Bundle名称不能为空");
                return null;
            }
            if ( BundleLoader.loadedBundles. ContainsKey (bundleName) )
            {
                return BundleLoader.loadedBundles[bundleName].bundle;
            }
            //手动加载资源包
            try
            {
                string path = Path. Combine (CASNFP_Globals. AssemblyPath, @"AssetBundles\", bundleName + ".ksp");
                if ( !File. Exists (path) )
                {
                    CASNFPLogger. Instance. LogError ($"资源包不存在: {path}");
                    return null;
                }
                AssetBundle bundle = AssetBundle. LoadFromFile (path);
                if ( bundle == null )
                {
                    CASNFPLogger. Instance. LogError ($"无法加载资源包: {path}");
                    return null;
                }
                return bundle;
            }
            catch ( System. Exception ex )
            {
                CASNFPLogger. Instance. LogError ($"加载资源包时发生错误: {ex. Message}");
                return null;
            }
        }
        public void UnloadBundle (AssetBundle bundle)
        {
            if ( bundle != null )
            {
                bundle. Unload (true);
            }
        }
    }
}
