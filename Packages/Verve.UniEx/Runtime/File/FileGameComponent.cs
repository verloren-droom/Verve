#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.File
{
    using UnityEngine;

    
    [System.Serializable, GameFeatureComponentMenu("Verve/File")]
    public sealed class FileGameComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("文件根目录")] private GameFeatureParameter<string> m_RootPath = new GameFeatureParameter<string>("/");
        public string RootPath => m_RootPath.Value;
    }
}

#endif