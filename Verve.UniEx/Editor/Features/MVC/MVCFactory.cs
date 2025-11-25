#if UNITY_EDITOR

namespace VerveEditor.MVC
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using Verve.UniEx.MVC;


    /// <summary>
    ///   <para>MVC工厂</para>
    /// </summary>
    internal static class MVCFactory
    {
        /// <summary>
        ///   <para>视图模版文件名</para>
        ///   <para>不包含.txt文件后缀</para>
        /// </summary>
        const string VIEW_TEMPLATE_FILENAME = "View.cs";
        
        
        /// <summary>
        ///   <para>创建视图</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/MVC/View")]
        private static void CreateNewGameFeatureComponentScript()
        {
            string[] guids = AssetDatabase.FindAssets($"{VIEW_TEMPLATE_FILENAME} t:TextAsset", new []{ UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MVCFactory).Assembly).assetPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                CreateViewScript(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"{VIEW_TEMPLATE_FILENAME} Template file not found", "OK");
            }
        }

        /// <summary>
        ///   <para>创建视图脚本</para>
        /// </summary>
        private static void CreateViewScript(string path)
        {
            try
            {
                var activityTypeNames = TypeCache.GetTypesDerivedFrom<ActivityBase>().Where(t => t.IsClass && !t.IsAbstract && t.IsPublic).Select(t => t.Name).ToArray();
            
                SingleSelectionDialog.Show("Select Activity", null, activityTypeNames, (ind) =>
                {
                    string tempTemplatePath = Path.Combine(Application.temporaryCachePath, $"{VIEW_TEMPLATE_FILENAME}.txt");
                    string templateContent = File.ReadAllText(path);
                        
                    string processedContent = templateContent.Replace("#ACTIVITY#", activityTypeNames[ind]);
                        
                    File.WriteAllText(tempTemplatePath, processedContent);
                        
                    ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                        tempTemplatePath, 
                        $"New{VIEW_TEMPLATE_FILENAME}"
                    );
                });
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create view script: {ex.Message}", "OK");
            }
        }
    }
}

#endif