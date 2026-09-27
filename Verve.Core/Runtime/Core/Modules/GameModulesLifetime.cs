#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>游戏入口。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>可选 Unity 生命周期适配；对象销毁时请求框架句柄释放，不转交模块所有权。</para>
        /// </summary>
        /// <param name="lifetime">生命周期。</param>
        /// <param name="options">容器创建选项。</param>
        public static GameModulesHandle CreateModules(Component lifetime, GameModulesOptions options = null)
        {
            if (lifetime == null) throw new ArgumentNullException(nameof(lifetime));
            return CreateModules(lifetime.gameObject, options);
        }

        /// <summary>
        ///   <para>每个 <see cref="UnityEngine.GameObject"/> 同时绑定一个容器的释放请求；首次绑定时必须激活。</para>
        /// </summary>
        /// <param name="lifetime">生命周期。</param>
        /// <param name="options">容器创建选项。</param>
        public static GameModulesHandle CreateModules(GameObject lifetime, GameModulesOptions options = null)
        {
            ThrowIfNotOnMainThread(nameof(CreateModules));
            if (lifetime == null) throw new ArgumentNullException(nameof(lifetime));
            var binding = lifetime.GetComponent<GameModulesLifetime>();
            if (binding == null)
            {
                if (!lifetime.activeInHierarchy)
                    throw new InvalidOperationException("Activate the lifetime object before binding a module container.");
                binding = lifetime.AddComponent<GameModulesLifetime>();
                binding.hideFlags = HideFlags.HideInInspector;
            }
            return binding.Create(options);
        }
    }

    /// <summary>
    ///   <para>Unity 生命周期组件；将宿主销毁转为容器释放请求。</para>
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("")]
    internal sealed class GameModulesLifetime : MonoBehaviour
    {
        /// <summary>
        ///   <para>句柄。</para>
        /// </summary>
        private GameModulesHandle m_Handle;

        /// <summary>
        ///   <para>创建实例。</para>
        /// </summary>
        /// <param name="options">容器创建选项。</param>
        internal GameModulesHandle Create(GameModulesOptions options)
        {
            if (m_Handle != null && !m_Handle.IsDisposed)
                throw new InvalidOperationException("This GameObject already has a module lifetime binding.");
            return m_Handle = Game.CreateModules(options: options);
        }

        /// <summary>
        ///   <para>销毁时清理。</para>
        /// </summary>
        private async void OnDestroy()
        {
            var handle = m_Handle;
            m_Handle = null;
            if (handle != null) await handle.DisposeAsync();
        }
    }
}

#endif