#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>页面返回栈；由 UI 模块维护实例生命周期。</para>
    /// </summary>
    public sealed class ViewStack
    {
        /// <summary>
        ///   <para>关闭页面的回调。</para>
        /// </summary>
        private Func<ViewBase, bool> m_Close;
        /// <summary>
        ///   <para>页面。</para>
        /// </summary>
        private readonly List<ViewBase> m_Views = new(8);

        /// <summary>
        ///   <para>由 <see cref="UIModule"/> 创建并注入关闭操作。</para>
        /// </summary>
        /// <param name="close">关闭页面的操作。</param>
        internal ViewStack(Func<ViewBase, bool> close) => m_Close = close ?? throw new ArgumentNullException(nameof(close));

        /// <summary>
        ///   <para>返回当前栈中的打开页面数量。</para>
        /// </summary>
        public int Count
        {
            get
            {
                Game.ThrowIfNotOnMainThread($"{nameof(ViewStack)}.{nameof(Count)}");
                if (!IsActive)
                {
                    return 0;
                }

                RemoveInvalidViews();
                return m_Views.Count;
            }
        }

        /// <summary>
        ///   <para>取得最近打开且仍有效的页面。</para>
        /// </summary>
        /// <param name="view">栈顶页面。</param>
        public bool TryPeek(out ViewBase view)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(ViewStack)}.{nameof(TryPeek)}");
            if (!IsActive)
            {
                view = null;
                return false;
            }

            while (m_Views.Count > 0)
            {
                var candidate = m_Views[^1];
                if (candidate != null && candidate.IsOpen && !candidate.IsReleased)
                {
                    view = candidate;
                    return true;
                }

                // 状态回调可能因 Unity 销毁顺序而晚于节点状态变化，读取时顺手丢弃失效项。
                m_Views.RemoveAt(m_Views.Count - 1);
            }

            view = null;
            return false;
        }

        /// <summary>
        ///   <para>关闭最近打开的页面。</para>
        /// </summary>
        public bool Back()
        {
            Game.ThrowIfNotOnMainThread($"{nameof(ViewStack)}.{nameof(Back)}");
            if (!TryPeek(out var view))
            {
                return false;
            }

            bool closed;
            try
            {
                closed = m_Close(view);
            }
            catch
            {
                // 关闭回调可能已经提交了关闭或释放，即使回调抛错也不能让失效页面残留在栈中。
                if (!view.IsOpen || view.IsReleased)
                {
                    Remove(view);
                }

                throw;
            }

            if (closed || !view.IsOpen || view.IsReleased)
            {
                Remove(view);
            }

            return closed;
        }

        /// <summary>
        ///   <para>按后进先出顺序关闭栈内全部页面。</para>
        /// </summary>
        public void Clear()
        {
            Game.ThrowIfNotOnMainThread($"{nameof(ViewStack)}.{nameof(Clear)}");
            if (!IsActive)
            {
                m_Views.Clear();
                return;
            }

            List<Exception> failures = null;
            while (TryPeek(out var view))
            {
                try
                {
                    m_Close(view);
                }
                catch (Exception exception)
                {
                    (failures ??= new List<Exception>()).Add(exception);
                }

                // 关闭失败且页面仍打开时保留栈顶，便于调用方修复后重试；继续处理会造成死循环。
                if (view.IsOpen && !view.IsReleased)
                {
                    break;
                }

                Remove(view);
            }

            if (failures == null)
            {
                return;
            }

            throw new AggregateException("关闭 ViewStack 页面时发生一个或多个错误。", failures);
        }

        /// <summary>
        ///   <para>由 <see cref="UIModule"/> 记录已打开页面；同类型实例按对象引用分别入栈。</para>
        /// </summary>
        /// <param name="view">已打开的页面。</param>
        internal void Push(ViewBase view)
        {
            if (!IsActive || view == null || !view.IsOpen)
            {
                return;
            }

            Remove(view);
            m_Views.Add(view);
        }

        /// <summary>
        ///   <para>由 <see cref="UIModule"/> 移除已关闭或已释放页面。</para>
        /// </summary>
        /// <param name="view">待移除的页面。</param>
        internal void Remove(ViewBase view)
        {
            if (view == null)
            {
                return;
            }

            for (var i = m_Views.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(m_Views[i], view))
                {
                    m_Views.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///   <para>模块卸载时断开关闭委托，防止外部保存的栈继续操作模块。</para>
        /// </summary>
        internal void Deactivate()
        {
            m_Views.Clear();
            m_Close = null;
        }

        /// <summary>
        ///   <para>是否激活。</para>
        /// </summary>
        private bool IsActive => m_Close != null;

        /// <summary>
        ///   <para>移除无效页面。</para>
        /// </summary>
        private void RemoveInvalidViews()
        {
            for (var i = m_Views.Count - 1; i >= 0; i--)
            {
                var view = m_Views[i];
                if (view == null || view.IsReleased || !view.IsOpen)
                {
                    m_Views.RemoveAt(i);
                }
            }
        }

    }

}

#endif
