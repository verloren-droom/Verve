namespace VerveEditor.UniEx
{
    
#if UNITY_EDITOR
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using System.Linq;
    using UnityEditor.PackageManager;
    
    
    [InitializeOnLoad]
    public static partial class FrameworkSettingEditor
    {
        public const string PACKAGE_NAME = "com.verloren-droom.verve-unity-extension";

        public static string FRAMEWORK_MACRO =>
            $"VERVE_UNIEX_{GetVersionNumber(GetPackageInfo(PACKAGE_NAME).version).Replace('.', '_')}_OR_NEWER"; 
        
        
        static FrameworkSettingEditor()
        {
            if (GetPackageInfo(PACKAGE_NAME) != null)
            {
                AddMacro(FRAMEWORK_MACRO);
            }
            
            // if (!File.Exists(Path.Combine(Application.dataPath, "csc.rsp")))
            // {
            //     File.AppendAllText(Path.Combine(Application.dataPath, "csc.rsp"), $"-define:{FRAMEWORK_MACRO}");
            // }
        }
        
        public static string GetVersionNumber(string version)
        {
            int index = version.IndexOfAny(new char[] { '-', '+', '_' });

            if (index >= 0)
            {
                return version.Substring(0, index);
            }

            return version;
        }
        
        /// <summary>
        /// 获取包信息
        /// </summary>
        /// <param name="packageName">包名</param>
        /// <returns></returns>
        public static UnityEditor.PackageManager.PackageInfo GetPackageInfo(string packageName)
        {
            var request = Client.List();
            while (!request.IsCompleted) { }

            if (request.Status == StatusCode.Success)
            {
                return request.Result.FirstOrDefault(package => package.name == packageName);
            }

            return null;
        }
        
        /// <summary>
        /// 添加宏
        /// </summary>
        /// <param name="macroName">宏名</param>
        public static void AddMacro(string macroName)
        {
            if (string.IsNullOrEmpty(macroName)) return;
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        
            if (!currentSymbols.Contains(macroName))
            {
                string newSymbols = currentSymbols + ";" + macroName;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newSymbols);
            }
        }
        
        /// <summary>
        /// 移除宏
        /// </summary>
        /// <param name="macroName">宏名</param>
        public static void RemoveMacro(string macroName)
        {
            if (string.IsNullOrEmpty(macroName)) return;
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        
            if (currentSymbols.Contains(macroName))
            {
                string newSymbols = currentSymbols.Replace(macroName + ";", "");
                newSymbols = newSymbols.Replace(";" + macroName, "");
                newSymbols = newSymbols.Replace(macroName, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newSymbols);
            }
        }
    }
#endif
    
}