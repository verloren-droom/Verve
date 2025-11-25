#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>加载器游戏功能组件</para>
    /// </summary>
    [Serializable, GameFeatureComponentMenu("Verve/Loader")]
    public sealed class LoaderGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("根文件夹")] private PathParameter m_RootFolder = new PathParameter();
        [SerializeField, Tooltip("远程资源服务器地址")] private GameFeatureParameter<string> m_RemoteServerUrl = new GameFeatureParameter<string>("http://[::]:8080/");
        [SerializeField, Tooltip("Manifest 包名（不带后缀）")] private GameFeatureParameter<string> m_ManifestBundleName = new GameFeatureParameter<string>("AssetBundleManifest");
        
        
        /// <summary>
        ///   <para>根文件夹</para>
        /// </summary>
        public string RootFolder => m_RootFolder.Value;
        
        /// <summary>
        ///   <para>远程资源服务器地址</para>
        /// </summary>
        public string RemoteServerUrl => m_RemoteServerUrl.Value;
        
        /// <summary>
        ///   <para>Manifest 包名（不带后缀）</para>
        /// </summary>
        public string ManifestBundleName => m_ManifestBundleName.Value;
    }
}

#endif