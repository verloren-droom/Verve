#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    /// <summary>
    ///   <para>游戏功能设置</para>
    ///   <para>用于管理和保存游戏功能模块和配置文件</para>
    /// </summary>
    [FilePath("ProjectSettings/Packages/com.verloren-droom.verve-unity-extension/GameFeaturesSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class GameFeaturesSettings : ScriptableSingleton<GameFeaturesSettings>
    {
        [SerializeField, Tooltip("扩展方法输出目录")] public string extensionOutputDir = "Assets/FeaturesGenerated";
        
        [SerializeField, HideInInspector, Tooltip("模块编辑器")]
#if UNITY_2019_4_OR_NEWER
        [SerializeReference]
#endif
        private List<GameFeatureModuleSettingDrawer> m_Drawers = new List<GameFeatureModuleSettingDrawer>();
        
        public IReadOnlyCollection<GameFeatureModuleSettingDrawer> Drawers => m_Drawers.AsReadOnly();

        
        private void OnEnable()
        {
            m_Drawers.RemoveAll(x => x == null);
            var drawerTypes = TypeCache.GetTypesDerivedFrom<GameFeatureModuleSettingDrawer>().Where(t => !t.IsAbstract && t.GetCustomAttribute<GameFeatureModuleSettingDrawerAttribute>() != null);
            foreach (var drawerType in drawerTypes)
            {
                GetOrCreateModuleEditor(drawerType);
            }
        }

        private void OnDisable()
        {
            Save();
        }

        /// <summary>
        ///   <para>保存设置</para>
        /// </summary>
        public void Save()
        {
            Save(true);
        }
        
        /// <summary>
        ///   <para>获取或创建模块编辑器</para>
        /// </summary>
        public GameFeatureModuleSettingDrawer GetOrCreateModuleEditor(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(GameFeatureModuleSettingDrawer).IsAssignableFrom(type)) throw new ArgumentException($"Type {type} is not a ModuleEditorDrawer");
            var existingDrawer = m_Drawers?.FirstOrDefault(x => x.GetType() == type);
            if (existingDrawer != null)
                return existingDrawer;
            var t = (GameFeatureModuleSettingDrawer)Activator.CreateInstance(type);
            // var t = (ModuleEditorDrawer)CreateInstance(type);
            // t.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            // t.name = ObjectNames.NicifyVariableName(type.Name);
            m_Drawers.Add(t);
            EditorUtility.SetDirty(this);
            Save();
            return t;
        }

        /// <summary>
        ///   <para>获取或创建模块编辑器</para>
        /// </summary>
        public T GetOrCreateModuleEditor<T>() where T : GameFeatureModuleSettingDrawer, new() => (T)GetOrCreateModuleEditor(typeof(T));

        /// <summary>
        ///   <para>尝试获取模块编辑器</para>
        /// </summary>
        public bool TryGetModuleEditorFromModule(Type moduleType, out GameFeatureModuleSettingDrawer drawer)
        {
            drawer = m_Drawers.FirstOrDefault(x => x.GetType().GetCustomAttribute<GameFeatureModuleSettingDrawerAttribute>()?.moduleType == moduleType);
            return drawer != null;
        }
    }
}

#endif