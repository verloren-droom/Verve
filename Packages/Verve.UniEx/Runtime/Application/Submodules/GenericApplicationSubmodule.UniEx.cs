#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    using UnityEngine;
#if UNITY_EDITOR
    using static UnityEditor.EditorApplication;
#endif

    /// <summary>
    /// Unity通用应用子模块
    /// </summary>
    [System.Serializable]
    public class GenericApplicationSubmodule : Verve.Application.ApplicationSubmodule
    {
        public override string ModuleName => "GenericApplication.UniEx";
        public override string DeviceId => SystemInfo.deviceUniqueIdentifier;
        public override string Version => Application.version;
        

        public override void OnModuleLoaded(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            base.OnModuleLoaded(dependencies);
            
            Application.quitting += OnApplicationQuitting;
            Application.focusChanged += OnApplicationFocusChanged;
            Application.lowMemory += OnApplicationLowMemory;
        }
        
        public override void OnModuleUnloaded()
        {
            base.OnModuleUnloaded();
            
            Application.quitting -= OnApplicationQuitting;
            Application.focusChanged -= OnApplicationFocusChanged;
            Application.lowMemory -= OnApplicationLowMemory;
        }

        public override void QuitApplication()
        {
#if UNITY_EDITOR
            isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        public override void RestartApplication()
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
            // TODO: 
        }
        
        protected virtual void OnApplicationFocusChanged(bool focus)
        {
            // TODO:
        }

        protected virtual void OnApplicationLowMemory()
        {
            // TODO:
        }
    }
}

#endif