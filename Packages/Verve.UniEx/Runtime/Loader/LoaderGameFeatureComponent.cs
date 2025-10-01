#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using Verve.UniEx;
    
    
    [Serializable, GameFeatureComponentMenu("Verve/Loader")]
    public sealed class LoaderGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("根文件夹")] private PathParameter m_RootFolder = new PathParameter();
        /// <summary> 根文件夹 </summary>
        public string RootFolder => m_RootFolder.Value;
        
        [SerializeField, Tooltip("远程资源服务器地址")] private GameFeatureParameter<string> m_RemoteServerUrl = new GameFeatureParameter<string>("http://[::]:8080/");
        /// <summary> 远程资源服务器地址 </summary>
        public string RemoteServerUrl => m_RemoteServerUrl.Value;
        
        [SerializeField, Tooltip("Manifest 包名（不带后缀）")] private GameFeatureParameter<string> m_ManifestBundleName = new GameFeatureParameter<string>("AssetBundleManifest");
        /// <summary> Manifest 包名（不带后缀） </summary>
        public string ManifestBundleName => m_ManifestBundleName.Value;
    }
}

#endif