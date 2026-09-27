#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>资源加载作用域；接管并批量释放加载句柄。</para>
    /// </summary>
    public sealed class AssetLoadScope : DisposableObject
    {
        /// <summary>
        ///   <para>待执行的释放回调。</para>
        /// </summary>
        private readonly List<Action> m_Releases = new();

        /// <summary>
        ///   <para>当前作用域托管的句柄数量。</para>
        /// </summary>
        public int Count => m_Releases.Count;

        /// <summary>
        ///   <para>托管一个资源加载句柄，并返回句柄中的资源结果。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="handle">要托管的资源加载句柄。</param>
        public TObject Track<TObject>(AssetLoadHandle<TObject> handle)
            where TObject : UnityEngine.Object
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            if (handle.IsReleased) throw new ObjectDisposedException(nameof(handle));

            var result = handle.Result;
            if (IsDisposed)
            {
                handle.Dispose();
                throw new ObjectDisposedException(nameof(AssetLoadScope));
            }

            var release = handle.TransferRelease();
            if (release != null) m_Releases.Add(release);
            return result;
        }

        /// <summary>
        ///   <para>释放全部句柄；清理完毕后抛出失败，不隐藏资源释放错误。</para>
        /// </summary>
        public void ReleaseAll() => ResourceUtility.ReleaseAll(m_Releases, release => release());

        /// <inheritdoc />
        protected override void OnDispose() => ReleaseAll();
    }
}

#endif
