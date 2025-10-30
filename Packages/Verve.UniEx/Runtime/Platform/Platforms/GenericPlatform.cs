#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
#if UNITY_EDITOR
    using static UnityEditor.EditorApplication;
#endif

    
    /// <summary>
    ///   <para>通用平台子模块</para>
    /// </summary>
    internal class GenericPlatform : GameFeatureSubmodule, IGamePlatform
    {
        public virtual string AppName => Application.productName;
        public virtual string AppVersion => Application.version;
        public virtual string PlatformName => Application.platform.ToString();
        public virtual string DeviceId => SystemInfo.deviceUniqueIdentifier;
        public float BatteryLevel => SystemInfo.batteryLevel;
        public string Language => Application.systemLanguage.ToString();
        
        private IGamePlatformFileSystem m_FileSystem = new GenericFileSystem();
        public IGamePlatformFileSystem FileSystem => m_FileSystem;

        public virtual void Quit()
        {
#if UNITY_EDITOR
            isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        public virtual void Restart()
        {
#if UNITY_EDITOR
            isPlaying = false;
            CallbackFunction callback = null;
            callback = () => 
            {
                delayCall -= callback;
                isPlaying = true;
            };
            delayCall += callback;
#endif
        }

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

        public virtual void HideProgressBar()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
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
        
        public virtual void Vibrate(long milliseconds = 100)
        {
            Handheld.Vibrate();
        }
        
        public virtual void SetKeepScreenOn(bool keepOn)
        {
            Screen.sleepTimeout = keepOn ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }

        public virtual bool RunInBackground
        {
            get => Application.runInBackground;
            set => Application.runInBackground = value;
        }
        
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

        public event Action<bool> OnApplicationFocus
        {
            add => Application.focusChanged += value;
            remove => Application.focusChanged -= value;
        }
        
        public event Action OnApplicationQuit
        {
            add => Application.quitting += value;
            remove => Application.quitting -= value;
        }
        
        private event Action m_OnLowMemory;
        
        public event Action OnLowMemory
        {
            add 
            { 
                m_OnLowMemory += value;
                Application.lowMemory += OnLowMemoryCallback;
            }
            remove 
            { 
                m_OnLowMemory -= value;
                Application.lowMemory -= OnLowMemoryCallback;
            }
        }
        
        private void OnLowMemoryCallback()
        {
            m_OnLowMemory?.Invoke();
        }
    }
}

#endif