#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;

    /// <summary>
    ///   <para>加载引用基类；通过引用计数管理底层资源。</para>
    /// </summary>
    internal abstract class LoadReference
    {
        /// <summary>
        ///   <para>引用数量。</para>
        /// </summary>
        private int m_RefCount;

        /// <summary>
        ///   <para>当前引用计数。</para>
        /// </summary>
        public int RefCount => m_RefCount;

        /// <summary>
        ///   <para>创建加载引用。</para>
        /// </summary>
        protected LoadReference() => m_RefCount = 1;

        /// <summary>
        ///   <para>增加一次资源引用。</para>
        /// </summary>
        public void Retain()
        {
            checked
            {
                m_RefCount++;
            }
        }

        /// <summary>
        ///   <para>释放一次资源引用，并返回释放后的引用计数。</para>
        /// </summary>
        public int Release()
        {
            if (m_RefCount <= 0)
            {
                throw new InvalidOperationException("Loader reference count is already zero.");
            }

            m_RefCount--;
            return m_RefCount;
        }
    }

    /// <summary>
    ///   <para>资源加载引用；为加载句柄提供资源与释放入口。</para>
    /// </summary>
    internal abstract class AssetLoadReference : LoadReference
    {
        /// <summary>
        ///   <para>资源实例。</para>
        /// </summary>
        public UnityEngine.Object Asset { get; }

        /// <summary>
        ///   <para>Unity 实例编号。</para>
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        ///   <para>创建资源加载引用。</para>
        /// </summary>
        /// <param name="asset">资源。</param>
        protected AssetLoadReference(UnityEngine.Object asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));

            Asset = asset;
            InstanceId = asset.GetInstanceID();
        }

        /// <summary>
        ///   <para>释放当前记录持有的底层加载器资源。</para>
        /// </summary>
        public abstract void ReleaseResources();
    }
}

#endif
