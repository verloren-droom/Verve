#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;

    /// <summary>
    ///   <para>资源作用域组件；随宿主销毁自动释放加载句柄。</para>
    /// </summary>
    [DefaultExecutionOrder(32000), DisallowMultipleComponent, AddComponentMenu("")]
    internal sealed class AssetLoadScopeComponent : MonoBehaviour
    {
        /// <summary>
        ///   <para>作用域。</para>
        /// </summary>
        private AssetLoadScope m_Scope;

        /// <summary>
        ///   <para>当前 <see cref="UnityEngine.GameObject"/> 绑定的资源加载作用域。</para>
        /// </summary>
        public AssetLoadScope Scope => m_Scope ??= new AssetLoadScope();

        /// <summary>
        ///   <para>初始化。</para>
        /// </summary>
        private void Awake() => hideFlags |= HideFlags.HideInInspector;

        /// <summary>
        ///   <para>销毁时清理。</para>
        /// </summary>
        private void OnDestroy()
        {
            m_Scope?.Dispose();
            m_Scope = null;
        }
    }
}

#endif