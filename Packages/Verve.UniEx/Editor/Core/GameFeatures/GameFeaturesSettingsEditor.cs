#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using Verve.UniEx;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    

    [CustomEditor(typeof(GameFeaturesSettings))]
    internal sealed class GameFeaturesSettingsEditor : Editor
    {
        private GameFeaturesSettings m_Settings;
        private SerializedProperty m_ComponentProfileProperty;
        private SerializedProperty m_ModuleProfileProperty;
        
        private readonly TabPagerBox m_TabPagerBox = new TabPagerBox(0);

        private static readonly string[] s_ExcludedFields =
        {
            "m_Script",
            "m_ModuleProfile",
            "m_ComponentProfile",
            "extensionOutputDir",
            "m_Drawers",
        };

        private static class Styles
        {
            public static GUIContent ComponentProfile { get; } = EditorGUIUtility.TrTextContent("Component Profile", "The profile containing all game feature components.");
            public static GUIContent ModuleProfile { get; } = EditorGUIUtility.TrTextContent("Module Profile", "The profile containing all game feature modules.");
            public static GUIContent NewLabel { get; } = EditorGUIUtility.TrTextContent("New", "Create a new component profile.");
            public static GUIContent NewModuleDataLabel { get; } = EditorGUIUtility.TrTextContent("New", "Create a new module profile.");
            public static string NoProfileMessage { get; } = L10n.Tr("Please select or create a new GameFeatures profile to begin applying features to the game.");
            public static string NoModuleDataMessage { get; } = L10n.Tr("Please select or create a new GameFeatures module data to manage game feature modules.");
            public static GUIContent GenerateButton { get; } = EditorGUIUtility.TrTextContent("Generate", "Generate extension methods for all modules.");
            public static GUIContent BrowseButton { get; } = EditorGUIUtility.TrTextContent("Browse", "Select a directory to save generated files.");
            public static GUIContent ExtensionGeneratorTitle { get; } = EditorGUIUtility.TrTextContent("Extension Generator", "Generate extension methods for modules.");
        }

        private void OnEnable()
        {
            m_Settings = target as GameFeaturesSettings;
            if (m_Settings == null || target == null) return;
            
            m_ComponentProfileProperty = serializedObject.FindProperty("m_ComponentProfile");
            m_ModuleProfileProperty = serializedObject.FindProperty("m_ModuleProfile");
        }

        public override void OnInspectorGUI()
        {
            if (m_Settings == null || target == null) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            DrawPropertiesExcluding(serializedObject, s_ExcludedFields);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(Styles.ModuleProfile);
                
                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    var newModuleProfile = EditorGUILayout.ObjectField(
                        m_ModuleProfileProperty.objectReferenceValue, 
                        typeof(GameFeatureModuleProfile), 
                        false) as GameFeatureModuleProfile;
            
                    if (changeCheckScope.changed)
                    {
                        m_ModuleProfileProperty.objectReferenceValue = newModuleProfile;
                    }
                }
        
                if (GUILayout.Button(Styles.NewModuleDataLabel, EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    string defaultPath = "Assets/Resources";
                    if (!AssetDatabase.IsValidFolder(defaultPath))
                    {
                        defaultPath = "Assets";
                    }
                    
                    string profilePath = EditorUtility.SaveFilePanelInProject("Save Game Feature Module Profile", "GameFeatureModuleProfile", "asset", "Save the Game Feature Module Profile asset");
                    if (!string.IsNullOrEmpty(profilePath))
                    {
                        var profile = ScriptableObject.CreateInstance<GameFeatureModuleProfile>();
                        profile.name = "Game Feature Module Profile";
                        AssetDatabase.CreateAsset(profile, profilePath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        
                        m_ModuleProfileProperty.objectReferenceValue = profile;
                    }
                }
            }
            
            if (m_ModuleProfileProperty.objectReferenceValue != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(Styles.ComponentProfile);
                    
                    using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                    {
                        var newComponentProfile = EditorGUILayout.ObjectField(
                            m_ComponentProfileProperty.objectReferenceValue, 
                            typeof(GameFeatureComponentProfile), 
                            false) as GameFeatureComponentProfile;
            
                        if (changeCheckScope.changed)
                        {
                            m_ComponentProfileProperty.objectReferenceValue = newComponentProfile;
                        }
                    }
        
                    if (GUILayout.Button(Styles.NewLabel, EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        string defaultPath = "Assets/Resources";
                        if (!AssetDatabase.IsValidFolder(defaultPath))
                        {
                            defaultPath = "Assets";
                        }
                    
                        string profilePath = EditorUtility.SaveFilePanelInProject("Save Game Feature Component Profile", "GameFeatureComponentProfile", "asset", "Save the Game Feature Component Profile asset");
                        if (!string.IsNullOrEmpty(profilePath))
                        {
                            var profile = ScriptableObject.CreateInstance<GameFeatureComponentProfile>();
                            profile.name = "Game Feature Component Profile";
                            AssetDatabase.CreateAsset(profile, profilePath);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        
                            m_ComponentProfileProperty.objectReferenceValue = profile;
                        }
                    }
                }

                EditorGUILayout.Space();
                
                if (m_ComponentProfileProperty.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(Styles.NoProfileMessage, MessageType.Info);
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(Styles.ExtensionGeneratorTitle);
                    m_Settings.extensionOutputDir = EditorGUILayout.TextField(m_Settings.extensionOutputDir, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button(Styles.BrowseButton, EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        string selectedPath = EditorUtility.SaveFolderPanel("Select Save Directory", m_Settings.extensionOutputDir, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            m_Settings.extensionOutputDir = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        }
                    }
                    if (GUILayout.Button(Styles.GenerateButton, EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        BridgeExtensionGenerator.GenerateAllBridgeExtensions(m_ModuleProfileProperty.objectReferenceValue as GameFeatureModuleProfile, m_Settings.extensionOutputDir);
                    }
                }
                
                DrawModuleEditorSettings();
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.NoModuleDataMessage, MessageType.Warning);
            }

            
            if (EditorGUI.EndChangeCheck())
            {
                GameFeaturesRunner.Instance.SetProfiles(GameFeaturesSettings.instance.ModuleProfile, GameFeaturesSettings.instance.ComponentProfile);
                GameFeaturesRunner.Instance.skipRuntimeDependencyChecks = GameFeaturesSettings.instance.SkipRuntimeDependencyChecks;
                serializedObject.ApplyModifiedProperties();
                GameFeaturesSettings.instance.Save();
            }
        }
        
        /// <summary>
        /// 绘制模块编辑器设置
        /// </summary>
        private void DrawModuleEditorSettings()
        {
            var drawerTypes = TypeCache.GetTypesDerivedFrom<ModuleEditorDrawer>().Where(t => !t.IsAbstract && t.GetCustomAttribute<ModuleEditorDrawerAttribute>() != null);
            foreach (var drawerType in drawerTypes)
            {
                m_Settings.GetOrCreateModuleEditor(drawerType);
            }
            
            if (m_Settings.Drawers == null || m_Settings.Drawers.Count == 0)
            {
                EditorGUILayout.HelpBox("No module editor settings available.", MessageType.Info);
                return;
            }
            
            var tabTitles = new List<string>();
            var drawersList = m_Settings.Drawers.ToList();
            
            var activeModuleTypes = new HashSet<Type>();
            if (m_Settings.ModuleProfile != null && m_Settings.ModuleProfile.Modules != null)
            {
                foreach (var module in m_Settings.ModuleProfile.Modules)
                {
                    if (module != null)
                    {
                        activeModuleTypes.Add(module.GetType());
                    }
                }
            }
            
            var moduleTypeToDrawer = new Dictionary<Type, ModuleEditorDrawer>();
            foreach (var drawer in drawersList)
            {
                if (drawer != null)
                {
                    var attr = drawer.GetType().GetCustomAttribute<ModuleEditorDrawerAttribute>();
                    if (attr != null)
                    {
                        moduleTypeToDrawer[attr.moduleType] = drawer;
                    }
                }
            }
            
            var drawerActivationStatus = new List<bool>();
            foreach (var drawer in drawersList)
            {
                if (drawer != null)
                {
                    var drawerType = drawer.GetType();
                    var attr = drawerType.GetCustomAttribute<ModuleEditorDrawerAttribute>();
                    bool isActive = attr != null && activeModuleTypes.Contains(attr.moduleType);
                    drawerActivationStatus.Add(isActive);
                    
                    var moduleName = ObjectNames.NicifyVariableName(drawerType.Name);
                    const string suffix = "Module";
                    if (moduleName.EndsWith(suffix))
                        moduleName = moduleName.Substring(0, moduleName.Length - suffix.Length);
                        
                    if (moduleName.Length > 20)
                    {
                        moduleName = $"{moduleName.Substring(0, 17)}...{moduleName.Substring(moduleName.Length - 3)}";
                    }

                    tabTitles.Add(moduleName);
                }
                else
                {
                    drawerActivationStatus.Add(false);
                    tabTitles.Add("Unknown");
                }
            }
            
            int selectedIndex = m_TabPagerBox.Begin(tabTitles.ToArray());
            
            if (selectedIndex >= 0 && selectedIndex < drawersList.Count)
            {
                var selectedDrawer = drawersList[selectedIndex];
                bool isDrawerActive = drawerActivationStatus[selectedIndex];
                
                if (selectedDrawer != null)
                {
                    using (new EditorGUI.DisabledGroupScope(!isDrawerActive))
                    {
                        EditorGUI.BeginChangeCheck();
                        selectedDrawer.OnGUI();
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(m_Settings);
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
        
            m_TabPagerBox.End();
        }
    }
}

#endif