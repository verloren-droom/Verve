#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Platform
{
    using System;
    using UnityEngine;
    using Verve.Platform;
    
    
    /// <summary>
    /// 通用平台子模块
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(PlatformGameFeature), Description = "通用平台")]
    public class GenericPlatform : GameFeatureSubmodule, IPlatform
    {
        public virtual void OpenUrl(string url) => Application.OpenURL(url);

        public virtual void ShowProgressBar(string title, string message, float progress)
        {
#if UNITY_EDITOR
            if (progress >= 1)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
                return;
            }
            UnityEditor.EditorUtility.DisplayProgressBar(title, message, progress);
#else
#endif
        }

        public virtual void ShowDialog(string title, string message, Action<bool> onResult, string okText = "确定", string cancelText = "取消")
        {
#if UNITY_EDITOR
            var ok = UnityEditor.EditorUtility.DisplayDialog(title, message, okText, cancelText);
            onResult?.Invoke(ok);
#else
#endif
        }

        public virtual void ShowDialog(string title, string message, string okText = "确定")
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(title, message, okText);
#else
#endif
        }

        public virtual void OpenFilePicker(Action<string> onFileSelected, string filter = "All files (*.*)|*.*")
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Open File", "", ParseFilter(filter));
            onFileSelected?.Invoke(path);
#else
#endif
        }

        public virtual void CopyToClipboard(string text) => GUIUtility.systemCopyBuffer = text;
        public virtual string GetClipboardText() => GUIUtility.systemCopyBuffer;
        public virtual string GetPersistentDataPath() => 
#if UNITY_EDITOR
            System.IO.Path.Combine(GetProjectPath(), ".Cache");
#else
            Application.persistentDataPath;
#endif
        
        public virtual string GetTemporaryCachePath() =>
#if UNITY_EDITOR
            System.IO.Path.Combine(GetProjectPath(), ".Temp");
#else
            Application.temporaryCachePath;
#endif
        
        public virtual string GetProjectPath() => Application.dataPath;
        
        protected string[] ParseFilter(string filter)
        {
            filter = filter.Trim();
            if (string.IsNullOrEmpty(filter)) 
                return Array.Empty<string>();

            try
            {
                string[] parts = filter.Split('|');
                string[] result = new string[parts.Length];
            
                for (int i = 0; i < parts.Length; i++)
                {
                    result[i] = parts[i].Trim();
                }
            
                return result;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}

#endif