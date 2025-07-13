#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Platform
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    /// Unity引擎通用平台子模块
    /// </summary>
    public class GenericPlatformSubmodule : Verve.Platform.PlatformSubmodule
    {
        public override string ModuleName => "GenericPlatform.UniEx";
        
        public override void OpenUrl(string url) => Application.OpenURL(url);

        public override void ShowDialog(string title, string message)
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.DisplayDialog(title, message, "确定");
#else
#endif
        }

        public override void OpenFilePicker(Action<string> onFileSelected, string filter = "All files (*.*)|*.*")
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Open File", "", ParseFilter(filter));
            onFileSelected?.Invoke(path);
#else
#endif
        }

        public override void CopyToClipboard(string text) => GUIUtility.systemCopyBuffer = text;
        public override string GetClipboardText() => GUIUtility.systemCopyBuffer;
        public override string GetPersistentDataPath() => 
#if UNITY_EDITOR
            System.IO.Path.Combine(Application.dataPath, ".Cache");
#else
            Application.persistentDataPath;
#endif
        
        public override string GetTemporaryCachePath() =>
#if UNITY_EDITOR
            System.IO.Path.Combine(Application.dataPath, ".Temp");
#else
            Application.temporaryCachePath;
#endif
    }
}

#endif