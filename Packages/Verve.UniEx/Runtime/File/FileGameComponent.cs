#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.File
{
    using UnityEngine;

    
    [System.Serializable, GameFeatureComponentMenu("Verve/File")]
    public sealed class FileGameComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("根文件夹")] private PathParameter m_RootFolder = new PathParameter();
        /// <summary> 根文件夹 </summary>
        public string RootFolder => m_RootFolder.Value;
    }
}

#endif