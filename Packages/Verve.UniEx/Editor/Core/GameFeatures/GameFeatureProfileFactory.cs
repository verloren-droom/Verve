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
    
    
    /// <summary>
    ///  <para>游戏特性配置文件工厂</para>
    /// </summary>
    internal static class GameFeatureProfileFactory
    {
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
        
        /// <summary>
        ///  <para>创建游戏功能组件</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/Game Feature Component")]
        private static void CreateNewGameFeatureComponentScript()
        {
            string[] guids = AssetDatabase.FindAssets("GameFeatureComponent.cs t:TextAsset", new []{ UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GameFeatureProfileFactory).Assembly).assetPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, "NewGameFeatureComponent.cs");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "GameFeatureComponent Template file not found", "OK");
            }
        }
        
        /// <summary>
        ///  <para>创建游戏功能模块</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/Game Feature Module")]
        private static void CreateNewGameFeatureModuleScript()
        {
            string[] guids = AssetDatabase.FindAssets("GameFeatureModule.cs t:TextAsset", new []{ UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GameFeatureProfileFactory).Assembly).assetPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, "NewGameFeatureModule.cs");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "GameFeatureModule Template file not found", "OK");
            }
        }
        
        /// <summary>
        ///  <para>创建游戏功能子模块</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/Game Feature Submodule")]
        private static void CreateNewGameFeatureSubmoduleScript()
        {
            string[] guids = AssetDatabase.FindAssets("GameFeatureSubmodule.cs t:TextAsset", new []{ UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GameFeatureProfileFactory).Assembly).assetPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                CreateSubmoduleScript(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "GameFeatureSubmodule Template file not found", "OK");
            }
        }
        
        /// <summary>
        ///  <para>创建子模块脚本</para>
        /// </summary>
        private static void CreateSubmoduleScript(string path)
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
                        string templateContent = File.ReadAllText(path);
                        
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