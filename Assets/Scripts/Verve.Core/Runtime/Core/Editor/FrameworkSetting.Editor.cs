
namespace Verve
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class FrameworkSettingEditor
    {
        static FrameworkSettingEditor()
        {
            AddMacro("VERVE_FRAMEWORK");
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
            
            EditorApplication.quitting += () =>
            {
                RemoveMacro(macroName);
            };
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