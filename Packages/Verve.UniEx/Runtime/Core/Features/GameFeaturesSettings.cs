#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System.IO;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditorInternal;
#endif
    using Object = UnityEngine.Object;

    
    /// <summary>
    /// 游戏功能设置 - 用于管理和保存游戏功能模块和配置文件
    /// </summary>
    public class GameFeaturesSettings : ScriptableObject
    {
        private const string k_SettingsPath = "ProjectSettings/GameFeaturesSettings.asset";
        private static GameFeaturesSettings s_Instance;
        [SerializeField, Tooltip("游戏功能模块配置文件")] private GameFeatureModuleProfile m_ModuleProfile;
        [SerializeField, Tooltip("游戏功能组件配置文件")] private GameFeatureComponentProfile m_ComponentProfile;
        [SerializeField, Tooltip("是否在运行时跳过依赖检查")] private bool m_SkipRuntimeDependencyChecks;
        [SerializeField, Tooltip("扩展方法输出目录")] public string ExtensionOutputDir = "Assets/FeaturesGenerated";
        
        public GameFeatureComponentProfile ComponentProfile => m_ComponentProfile;
        public GameFeatureModuleProfile ModuleProfile => m_ModuleProfile;
        public bool SkipRuntimeDependencyChecks => m_SkipRuntimeDependencyChecks;

        
        public static GameFeaturesSettings GetOrCreateSettings()
        {
            if (s_Instance != null)
                return s_Instance;
                
            Object[] objs = InternalEditorUtility.LoadSerializedFileAndForget(k_SettingsPath);
            if (objs.Length > 0)
            {
                s_Instance = objs[0] as GameFeaturesSettings;
            }
            else
            {
                s_Instance = CreateInstance<GameFeaturesSettings>();
                s_Instance.name = "Game Features Settings";

                Save();
            }
            
            return s_Instance;
        }
        
        public static void Save()
        {
#if UNITY_EDITOR
            if (s_Instance == null)
                return;
                
            string directoryName = Path.GetDirectoryName(k_SettingsPath);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            
            Object[] objs = new Object[] { s_Instance };
            InternalEditorUtility.SaveToSerializedFileAndForget(objs, k_SettingsPath, true);
#endif
        }
    }
}

#endif