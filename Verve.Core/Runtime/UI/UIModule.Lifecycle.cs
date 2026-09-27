#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///   <para>UI 模块。</para>
    /// </summary>
    sealed partial class UIModule
    {
        /// <inheritdoc />
        protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.Install");
            m_ViewRootLocked = false;
            m_Loader = context.GetDependency<ILoader>();
            m_Lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
            m_ViewStack = new ViewStack(Close);
            try
            {
                EnsureRoot();
                m_ModalLayer = new UIModalLayer(() => GetLayerRoot(UILayer.Overlay));
                return default;
            }
            catch (Exception failure)
            {
                try { Cleanup(); }
                catch (Exception cleanupFailure) { throw ExceptionUtility.Combine(failure, cleanupFailure); }
                throw;
            }
        }

        /// <inheritdoc />
        protected override async ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.Uninstall");
            var pending = Task.WhenAll(m_OpenOperations.ConvertAll(opening => opening.Completion.Task));
            Exception failure = null;
            try { m_Lifetime?.Cancel(); }
            catch (Exception exception) { failure = exception; }
            try { await pending; }
            catch (OperationCanceledException) when (pending.IsCanceled)
            {
                // 卸载期间取消属于正常生命周期行为。
            }
            catch (Exception exception)
            {
                failure = ExceptionUtility.Combine(failure, pending.Exception ?? exception);
            }

            ReleaseResources(failure);
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.Dispose");
            Exception failure = null;
            try { m_Lifetime?.Cancel(); }
            catch (Exception exception) { failure = exception; }
            ReleaseResources(failure);
        }

        /// <summary>
        ///   <para>释放页面与模块资源；完成后汇总错误。</para>
        /// </summary>
        /// <param name="failure">已有错误。</param>
        private void ReleaseResources(Exception failure)
        {
            try { ReleaseAllInternal(layer: null); }
            catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
            try { Cleanup(); }
            catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
            ExceptionUtility.Rethrow(failure);
        }

        /// <summary>
        ///   <para>清理。</para>
        /// </summary>
        private void Cleanup()
        {
            m_ViewEntries.Clear();
            m_ViewLookup.Clear();
            m_CreatingTypes.Clear();
            m_OpenOperations.Clear();

            m_Loader = null;
            m_CacheAccessOrder = 0;
            m_NextInstanceId = 0;
            m_ViewRootLocked = false;
            m_EvictionCandidates.Clear();
            m_IsEvictingCache = false;
            m_ViewStack?.Deactivate();
            m_ModalLayer?.Release();
            m_ModalLayer = null;
            m_Lifetime?.Dispose();
            m_Lifetime = null;
            if (m_OwnsViewRoot && m_ViewRoot != null)
            {
                UnityEngine.Object.Destroy(m_ViewRoot.gameObject);
            }
            else
            {
                DestroyOwnedHierarchy();
            }

            if (m_OwnsViewRoot)
            {
                m_ViewRoot = null;
                m_LayerRoots.Clear();
            }

            m_OwnsViewRoot = false;
        }
    }
}

#endif