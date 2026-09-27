#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>场景加载或卸载操作句柄。</para>
    /// </summary>
    public readonly struct SceneLoadHandle
    {
        /// <summary>
        ///   <para>激活回调。</para>
        /// </summary>
        private readonly Action m_ActivateAction;

        /// <summary>
        ///   <para>场景异步操作。</para>
        /// </summary>
        public AsyncOperation Operation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        ///   <para>操作关联的场景。</para>
        /// </summary>
        public Scene Scene { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }

        /// <summary>
        ///   <para>异步操作是否完成。</para>
        /// </summary>
        public bool IsDone { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Operation?.isDone ?? Scene.isLoaded; }

        /// <summary>
        ///   <para>关联场景是否有效且已加载。</para>
        /// </summary>
        public bool IsLoaded { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Scene.IsValid() && Scene.isLoaded; }

        /// <summary>
        ///   <para>创建包含异步操作、场景实例与自定义激活逻辑的场景句柄。</para>
        /// </summary>
        /// <param name="operation">场景异步操作。</param>
        /// <param name="scene">操作关联的场景。</param>
        /// <param name="activateAction">自定义场景激活逻辑。</param>
        public SceneLoadHandle(AsyncOperation operation, Scene scene = default, Action activateAction = null)
        {
            Operation = operation;
            Scene = scene;
            m_ActivateAction = activateAction;
        }

        /// <summary>
        ///   <para>创建仅包含场景实例的场景句柄。</para>
        /// </summary>
        /// <param name="scene">操作关联的场景。</param>
        public SceneLoadHandle(Scene scene)
            : this(null, scene) { }

        /// <summary>
        ///   <para>创建仅包含场景实例与自定义激活逻辑的场景句柄。</para>
        /// </summary>
        /// <param name="scene">操作关联的场景。</param>
        /// <param name="activateAction">自定义场景激活逻辑。</param>
        public SceneLoadHandle(Scene scene, Action activateAction)
            : this(null, scene, activateAction) { }

        /// <summary>
        ///   <para>允许此前暂停在 <b>0.9</b> 进度的场景加载继续激活。</para>
        /// </summary>
        public void Activate()
        {
            if (m_ActivateAction != null)
            {
                m_ActivateAction();
                return;
            }

            if (Operation != null)
            {
                Operation.allowSceneActivation = true;
            }
        }
    }
}

#endif