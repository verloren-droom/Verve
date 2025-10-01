#if UNITY_EDITOR

namespace VerveEditor.UniEx.HotFix
{
    using System;
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using Verve.UniEx;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    
    
    [Serializable, ModuleEditorDrawer(typeof(Verve.UniEx.ScriptRuntime.ScriptRuntimeGameFeature))]
    internal sealed class ScriptRuntimeGameModuleEditorSetting : ModuleEditorDrawer
    {
        [SerializeField] private string m_HotFixOutputDir = "Assets/GameFeaturesData";

        
        /// <summary>
        /// 准备热更GameFeature资源
        /// </summary>
        private void PrepareHotUpdateGameFeatures()
        {
            CreateHotUpdateOutputDirectory();
            
            var hotUpdateAssemblies = GetHotFixAssemblies();
            if (hotUpdateAssemblies.Count == 0)
            {
                Debug.LogWarning("未找到热更程序集配置，跳过GameFeature预处理");
                return;
            }
            
            var moduleProfile =
                CopyAssetToHotUpdateFolder(GameFeaturesRunner.Instance.ModuleProfile) as GameFeatureModuleProfile;
            var componentProfile =
                CopyAssetToHotUpdateFolder(GameFeaturesRunner.Instance.ComponentProfile) as GameFeatureComponentProfile;

            try
            {
                ProcessModuleProfile(moduleProfile, hotUpdateAssemblies);
                ProcessComponentProfile(componentProfile, hotUpdateAssemblies);
                GameFeaturesRunner.Instance.SetProfiles(moduleProfile, componentProfile);
            }
            catch (Exception e)
            {
                Debug.LogError($"GameFeature预处理发生异常: {e.Message}");
                Object.DestroyImmediate(moduleProfile);
                Object.DestroyImmediate(componentProfile);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("预处理完成", "GameFeature热更模块预处理已完成", "确定");
        }
        
         /// <summary>
         /// 获取热更程序集名称列表
         /// </summary>
         private HashSet<string> GetHotFixAssemblies()
         { 
             var hotUpdateAssemblies = new HashSet<string>();
             var settings = HybridCLR.Editor.Settings.HybridCLRSettings.Instance;
             if (settings?.enable == true)
             {
                 foreach (var assemblyName in settings.hotUpdateAssemblies)
                 {
                     hotUpdateAssemblies.Add(assemblyName);
                 }
                 foreach (var assemblyDef in settings.hotUpdateAssemblyDefinitions)
                 {
                     hotUpdateAssemblies.Add(assemblyDef.name);
                 }
                 CheckDependenciesAndThrow(hotUpdateAssemblies);
             }
             return hotUpdateAssemblies;
         }
         
         /// <summary>
         /// 检查程序集依赖关系
         /// </summary>
         private void CheckDependenciesAndThrow(HashSet<string> hotUpdateAssemblies)
         {
             foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
             {
                 string assemblyName = assembly.GetName().Name;
                 if (assemblyName.StartsWith("UnityEngine") || 
                     assemblyName.StartsWith("UnityEditor") ||
                     assemblyName.StartsWith("System") ||
                     assemblyName.StartsWith("mscorlib") ||
                     assemblyName.StartsWith("Microsoft") ||
                     assemblyName.StartsWith("Mono") ||
                     assemblyName.StartsWith("netstandard") ||
                     assemblyName.Contains("Editor"))
                     continue;
                
                 if (hotUpdateAssemblies.Contains(assemblyName))
                     continue;
                
                 var referencedAssemblies = assembly.GetReferencedAssemblies();
                 foreach (var referenced in referencedAssemblies)
                 {
                     string referencedName = referenced.Name;
                
                     if (hotUpdateAssemblies.Contains(referencedName))
                     {
                         Debug.LogError($"AOT程序集 {assembly.GetName().Name} 依赖了热更新程序集 {referencedName}，这会导致构建时类型解析错误。请重构代码避免此依赖关系。");
                     }
                 }
             }
         }
        
        /// <summary>
        /// 创建热更输出目录
        /// </summary>
        private void CreateHotUpdateOutputDirectory()
        {
            if (!AssetDatabase.IsValidFolder(m_HotFixOutputDir))
            {
                string fullPath = Path.GetFullPath(m_HotFixOutputDir);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    AssetDatabase.Refresh();
                }
            }
        }
        
        /// <summary>
        /// 处理模块配置文件
        /// </summary>
        private int ProcessModuleProfile(GameFeatureModuleProfile profile, HashSet<string> hotUpdateAssemblies)
        {
            var modulesToRemove = new List<GameFeatureModule>();
            
            foreach (GameFeatureModule module in profile.Modules)
            {
                if (module == null) continue;
                
                var moduleAssemblyName = module.GetType().Assembly.GetName().Name;
                if (hotUpdateAssemblies.Contains(moduleAssemblyName))
                {
                    modulesToRemove.Add(module);
                }
            }
            
            if (modulesToRemove.Count == 0) return 0;
            int processedCount = 0;
            
            // string modulesFolder = Path.Combine(GameFeaturesSettings.GetOrCreateSettings().hotFixOutputDir, "Modules");
            // if (!AssetDatabase.IsValidFolder(modulesFolder))
            // {
            //     string fullPath = Path.GetFullPath(modulesFolder);
            //     if (!Directory.Exists(fullPath))
            //     {
            //         Directory.CreateDirectory(fullPath);
            //         AssetDatabase.Refresh();
            //     }
            // }

            foreach (var module in modulesToRemove)
            {
                var asset = CopyAssetToHotUpdateFolder(module);
                if (asset == null) continue;
                // asset.name = module.name;
                profile.Remove(module);
                processedCount++;
            }
            
            return processedCount;
        }
        
        /// <summary>
        /// 处理组件配置文件
        /// </summary>
        private int ProcessComponentProfile(GameFeatureComponentProfile profile, HashSet<string> hotUpdateAssemblies)
        {
            var componentsToRemove = new List<GameFeatureComponent>();
            
            foreach (GameFeatureComponent component in profile.Components)
            {
                if (component == null) continue;
                
                var componentAssemblyName = component.GetType().Assembly.GetName().Name;
                if (hotUpdateAssemblies.Contains(componentAssemblyName))
                {
                    componentsToRemove.Add(component);
                }
            }
            
            if (componentsToRemove.Count == 0) return 0;
            int processedCount = 0;
            
            // string modulesFolder = Path.Combine(GameFeaturesSettings.GetOrCreateSettings().hotFixOutputDir, "Components");
            // if (!AssetDatabase.IsValidFolder(modulesFolder))
            // {
            //     string fullPath = Path.GetFullPath(modulesFolder);
            //     if (!Directory.Exists(fullPath))
            //     {
            //         Directory.CreateDirectory(fullPath);
            //         AssetDatabase.Refresh();
            //     }
            // }

            foreach (var component in componentsToRemove)
            {
                var asset = CopyAssetToHotUpdateFolder(component);
                if (asset == null) continue;
                // asset.name = component.name;
                profile.Remove(component.GetType());
                processedCount++;
            }
            
            return processedCount;
        }
        
        /// <summary>
        /// 复制资源到热更目录
        /// </summary>
        private ScriptableObject CopyAssetToHotUpdateFolder(ScriptableObject asset)
        {
            if (asset == null || !AssetDatabase.Contains(asset)) return null;
            
            string currentPath = AssetDatabase.GetAssetPath(asset);
            string fileName = asset.GetType().Name + ".asset";
            string newPath = Path.Combine(m_HotFixOutputDir, fileName).Replace("\\", "/");
            
            // newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
            
            var assetCopy = ScriptableObject.Instantiate(asset);
    
            AssetDatabase.CreateAsset(assetCopy, newPath);
            Debug.Log($"创建资产: {asset.name} -> {newPath}");
            return assetCopy;
        }

        public override bool OnGUI()
        {
            m_HotFixOutputDir = EditorGUILayout.TextField(
                new GUIContent("输出目录:", "热更资源输出目录"), 
                m_HotFixOutputDir);
            
            if (GUILayout.Button("预处理热更GameFeature资源")) 
            {
                PrepareHotUpdateGameFeatures();
            }
            return true;
        }
    }
}

#endif