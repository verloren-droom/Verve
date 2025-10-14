#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using Verve.UniEx;
    using System.Linq;
    using System.Reflection;
    
    
    internal static class GameFeatureProfileFactory
    {
        private static string s_ComponentTemplatePath { get; } = Path.Combine("Packages/Verve.UniEx/Editor/Core/Features/Templates/GameFeatureComponent.cs.txt");
        private static string s_ModuleTemplatePath { get; } = Path.Combine("Packages/Verve.UniEx/Editor/Core/Features/Templates/GameFeatureModule.cs.txt");
        private static string s_SubmoduleTemplatePath { get; } = Path.Combine("Packages/Verve.UniEx/Editor/Core/Features/Templates/GameFeatureSubmodule.cs.txt");
        
        
        public static GameFeatureComponentProfile CreateGameFeaturesProfile(string path = null, bool saveAsset = true)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                path = "Assets";
            }

            var profile = ScriptableObject.CreateInstance<GameFeatureComponentProfile>();
            AssetDatabase.CreateAsset(profile, Path.Combine(path, "GameFeaturesProfile.asset"));
            if (saveAsset)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return profile;
        }
        
        public static T CreateGameFeatureComponent<T>(GameFeatureComponentProfile profile, bool overrides = false, bool saveAsset = true)
            where T : GameFeatureComponent 
            => (T)CreateGameFeatureComponent(typeof(T), profile, overrides, saveAsset);
        
        public static GameFeatureComponent CreateGameFeatureComponent(Type type, GameFeatureComponentProfile profile, bool overrides = false, bool saveAsset = true)
        {
            var comp = profile.Add(type, overrides);
            if (EditorUtility.IsPersistent(profile))
            {
                AssetDatabase.AddObjectToAsset(comp, profile);
            }

            if (saveAsset && EditorUtility.IsPersistent(profile))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return comp;
        }
        
        [MenuItem("Assets/Create/Verve/Game Feature Component")]
        private static void CreateNewGameFeatureComponentScript()
        {
            if (!File.Exists(s_ComponentTemplatePath))
            {
                EditorUtility.DisplayDialog("Error", $"Template file not found: {s_ComponentTemplatePath}", "OK");
                return;
            }
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(s_ComponentTemplatePath, "NewGameFeatureComponent.cs");
        }
        
        [MenuItem("Assets/Create/Verve/Game Feature Module")]
        private static void CreateNewGameFeatureModuleScript()
        {
            if (!File.Exists(s_ModuleTemplatePath))
            {
                EditorUtility.DisplayDialog("Error", $"Template file not found: {s_ModuleTemplatePath}", "OK");
                return;
            }
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(s_ModuleTemplatePath, "NewGameFeatureModule.cs");
        }
        
        [MenuItem("Assets/Create/Verve/Game Feature Submodule")]
        private static void CreateNewGameFeatureSubmoduleScript()
        {
            if (!File.Exists(s_SubmoduleTemplatePath))
            {
                EditorUtility.DisplayDialog("Error", $"Template file not found: {s_SubmoduleTemplatePath}", "OK");
                return;
            }

            CreateSubmoduleScript();
        }
        
        /// <summary>
        /// 创建子模块脚本
        /// </summary>
        private static void CreateSubmoduleScript()
        {
            try
            {
                var moduleTypeNames = TypeCache.GetTypesDerivedFrom<GameFeatureModule>().Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && t.GetCustomAttribute<GameFeatureModuleAttribute>() != null).Select(t => t.Name).ToArray();

                SingleSelectionDialog.Show("Select Belong to Module", null, moduleTypeNames, (selectedModuleIndex) =>
                {
                    var componentTypeNames = TypeCache.GetTypesDerivedFrom<GameFeatureComponent>().Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && t.GetCustomAttribute<GameFeatureComponentMenuAttribute>() != null).Select(t => t.Name).ToArray();
                    
                    string[] componentOptions = new string[componentTypeNames.Length + 1];
                    componentOptions[0] = "None";
                    Array.Copy(componentTypeNames, 0, componentOptions, 1, componentTypeNames.Length);
                    
                    SingleSelectionDialog.Show("Select Bind Component", null, componentOptions, (selectedCompIndex) =>
                    {
                        string tempTemplatePath = Path.Combine(Application.temporaryCachePath, "GameFeatureSubmodule.cs.txt");
                        string templateContent = File.ReadAllText(s_SubmoduleTemplatePath);
                        
                        string processedContent = templateContent
                            .Replace("#MODULE#", moduleTypeNames[selectedModuleIndex])
                            .Replace("#COMPONENT#", selectedCompIndex > 0 ? $"<{componentOptions[selectedCompIndex]}>" : "");
                        
                        File.WriteAllText(tempTemplatePath, processedContent);
                        
                        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                            tempTemplatePath, 
                            "NewGameFeatureSubmodule.cs"
                        );
        
                        // if (File.Exists(tempTemplatePath))
                        // {
                        //     File.Delete(tempTemplatePath);
                        // }
                    });
                });
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create submodule script: {ex.Message}", "OK");
            }
        }
    }
}

#endif