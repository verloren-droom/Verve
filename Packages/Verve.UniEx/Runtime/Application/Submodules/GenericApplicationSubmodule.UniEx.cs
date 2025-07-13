#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    using UnityEngine;


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
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        public override void RestartApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.delayCall += () => 
            {
                UnityEditor.EditorApplication.isPlaying = true;
            };
#endif
        }

        protected virtual void OnApplicationQuitting()
        {
            
        }
        
        protected virtual void OnApplicationFocusChanged(bool focus)
        {

        }

        protected virtual void OnApplicationLowMemory()
        {
            
        }
    }
}

#endif