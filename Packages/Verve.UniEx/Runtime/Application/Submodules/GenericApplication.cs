#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Application
{
    using UnityEngine;
    using Verve.Application;
    using System.Collections;
#if UNITY_EDITOR
    using static UnityEditor.EditorApplication;
#endif

    /// <summary>
    /// Unity通用应用子模块
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(ApplicationGameFeature), Description = "Unity通用应用子模块")]
    public class GenericApplication : GameFeatureSubmodule, IApplication
    {
        public virtual string DeviceId => SystemInfo.deviceUniqueIdentifier;
        public virtual string Version => Application.version;

        protected override IEnumerator OnStartup()
        {
            Application.quitting += OnApplicationQuitting;
            Application.focusChanged += OnApplicationFocusChanged;
            Application.lowMemory += OnApplicationLowMemory;
            Application.logMessageReceived += OnApplicationLogMessageReceived;

            return base.OnStartup();
        }

        protected override void OnShutdown()
        {
            Application.quitting -= OnApplicationQuitting;
            Application.focusChanged -= OnApplicationFocusChanged;
            Application.lowMemory -= OnApplicationLowMemory;
            Application.logMessageReceived -= OnApplicationLogMessageReceived;
            base.OnShutdown();
        }

        public virtual void QuitApplication()
        {
#if UNITY_EDITOR
            isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        public virtual void RestartApplication()
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

        protected virtual void OnApplicationQuitting()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }
        
        protected virtual void OnApplicationFocusChanged(bool focus)
        {
            
        }

        protected virtual void OnApplicationLowMemory()
        {
            
        }
        
        protected virtual void OnApplicationLogMessageReceived(string condition, string stacktrace, LogType type)
        {

        }
    }
}

#endif