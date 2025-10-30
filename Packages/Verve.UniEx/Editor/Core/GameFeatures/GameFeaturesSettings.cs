#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using System.IO;
    using Verve.UniEx;
    using UnityEditor;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using UnityEditorInternal;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    
    /// <summary>
    ///   <para>游戏功能设置</para>
    ///   <para>用于管理和保存游戏功能模块和配置文件</para>
    /// </summary>
    [FilePath("ProjectSettings/Packages/com.verloren-droom.verve-unity-extension/GameFeaturesSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class GameFeaturesSettings : ScriptableSingleton<GameFeaturesSettings>
    {
        [SerializeField, Tooltip("游戏功能模块配置文件")] private GameFeatureModuleProfile m_ModuleProfile;
        [SerializeField, Tooltip("游戏功能组件配置文件")] private GameFeatureComponentProfile m_ComponentProfile;
        [SerializeField, Tooltip("是否在运行时跳过依赖检查")] private bool m_SkipRuntimeDependencyChecks;
        [SerializeField, Tooltip("扩展方法输出目录")] public string extensionOutputDir = "Assets/FeaturesGenerated";
        
        public GameFeatureComponentProfile ComponentProfile => m_ComponentProfile;
        public GameFeatureModuleProfile ModuleProfile => m_ModuleProfile;
        public bool SkipRuntimeDependencyChecks => m_SkipRuntimeDependencyChecks;
        
        [SerializeField, Tooltip("模块编辑器"), SerializeReference] private List<ModuleEditorDrawer> m_Drawers = new List<ModuleEditorDrawer>();
        public IReadOnlyCollection<ModuleEditorDrawer> Drawers => m_Drawers.AsReadOnly();

        
        private void OnEnable()
        {
            m_Drawers.RemoveAll(x => x == null);
        }

        private void OnDisable()
        {
            Save();
        }

        /// <summary>
        ///   <para>获取或创建模块编辑器</para>
        /// </summary>
        public ModuleEditorDrawer GetOrCreateModuleEditor(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(ModuleEditorDrawer).IsAssignableFrom(type)) throw new ArgumentException($"Type {type} is not a ModuleEditorDrawer");
            var existingDrawer = m_Drawers?.FirstOrDefault(x => x.GetType() == type);
            if (existingDrawer != null) 
                return existingDrawer;
            var t = (ModuleEditorDrawer)Activator.CreateInstance(type);
            // var t = (ModuleEditorDrawer)CreateInstance(type);
            // t.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            // t.name = ObjectNames.NicifyVariableName(type.Name);
            m_Drawers.Add(t);
            EditorUtility.SetDirty(this);
            Save();
            return t;
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
        public T GetOrCreateModuleEditor<T>() where T : ModuleEditorDrawer, new() => (T)GetOrCreateModuleEditor(typeof(T));
    }
}

#endif