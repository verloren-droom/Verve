#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.ResourceManagement.ResourceProviders;
    
    
    /// <summary>
    /// 场景加载回调上下文
    /// </summary>
    public struct SceneLoaderCallbackContext
    {
        private AsyncOperation m_AsyncOperation;
        private readonly SceneInstance m_SceneInstance;
        private Scene m_Scene;
        
        public bool IsDone => m_AsyncOperation?.isDone ?? false;
        public bool IsLoaded => m_Scene.isLoaded;

        public SceneLoaderCallbackContext(AsyncOperation operation) : this()
        {
            m_AsyncOperation = operation;
        }

        public SceneLoaderCallbackContext(SceneInstance instance) : this()
        {
            m_SceneInstance = instance;
            m_Scene = m_SceneInstance.Scene;
        }

        public void Activate()
        {
            m_AsyncOperation ??= m_SceneInstance.ActivateAsync();
            
            if (m_AsyncOperation != null)
            {
                m_AsyncOperation.allowSceneActivation = true;
            }
        }

        public void Deactivate()
        {
            // TODO: 
        }
    }
}
    
#endif