#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx.Loader
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.ResourceManagement.ResourceProviders;
    
    
    /// <summary>
    ///   <para>场景加载回调上下文</para>
    /// </summary>
    public struct SceneLoaderCallbackContext
    {
        private AsyncOperation m_AsyncOperation;
        private readonly SceneInstance m_SceneInstance;
        private Scene m_Scene;
        
        /// <summary>
        ///   <para>是否完成</para>
        /// </summary>
        public bool IsDone => m_AsyncOperation?.isDone ?? false;
        
        /// <summary>
        ///   <para>是否已加载</para>
        /// </summary>
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

        /// <summary>
        ///   <para>激活场景</para>
        /// </summary>
        public void Activate()
        {
            m_AsyncOperation ??= m_SceneInstance.ActivateAsync();
            
            if (m_AsyncOperation != null)
            {
                m_AsyncOperation.allowSceneActivation = true;
            }
        }

        /// <summary>
        ///   <para>停用场景</para>
        /// </summary>
        public void Deactivate()
        {
            // TODO: 
        }
    }
}
    
#endif