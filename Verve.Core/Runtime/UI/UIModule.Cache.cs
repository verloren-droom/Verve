#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>UI 模块。</para>
    /// </summary>
    sealed partial class UIModule
    {
        /// <summary>
        ///   <para>记录 <see cref="UIViewCacheMode.KeepAlive"/> 页面最近一次使用顺序。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private void MarkKeepAliveUsed(ViewEntry entry)
        {
            if (entry == null || entry.Options.CacheMode != UIViewCacheMode.KeepAlive ||
                entry.View == null || entry.View.IsReleased)
            {
                return;
            }

            entry.LastAccessOrder = ++m_CacheAccessOrder;
        }

        /// <summary>
        ///   <para>按数量淘汰已关闭的 <see cref="UIViewCacheMode.KeepAlive"/> 页面。 只在 UI 操作后执行，避免缓存管理产生每帧开销。</para>
        /// </summary>
        private void TrimKeepAliveCache()
        {
            if (m_IsEvictingCache || m_KeepAliveLimit <= 0)
            {
                return;
            }

            List<Exception> failures = null;
            m_IsEvictingCache = true;
            try
            {
                var candidates = m_EvictionCandidates;
                candidates.Clear();
                foreach (var entries in m_ViewEntries.Values)
                {
                    foreach (var entry in entries)
                    {
                        if (entry == null || entry.Options.CacheMode != UIViewCacheMode.KeepAlive ||
                            !IsCurrentView(entry) || entry.View == null || entry.View.IsReleased ||
                            entry.View.IsOpen ||
                            entry.View.HasAsyncOperation ||
                            entry.View.Component == null)
                        {
                            continue;
                        }

                        candidates.Add(entry);
                    }
                }

                if (candidates.Count <= m_KeepAliveLimit)
                {
                    return;
                }

                candidates.Sort(CompareKeepAliveEntries);
                var remaining = candidates.Count;
                for (var i = 0; i < candidates.Count; i++)
                {
                    var entry = candidates[i];
                    var overLimit = remaining > m_KeepAliveLimit;
                    remaining--;
                    if (!overLimit)
                    {
                        continue;
                    }

                    // 回调可能在清理过程中改变页面状态，释放前再次确认条目仍有效。
                    if (!IsCurrentView(entry) || entry.View == null || entry.View.IsReleased ||
                        entry.View.IsOpen)
                    {
                        continue;
                    }

                    try
                    {
                        ReleaseEntry(entry);
                    }
                    catch (Exception exception)
                    {
                        // 条目已从缓存移除，继续尝试清理其余页面。
                        ExceptionUtility.Add(ref failures, exception);
                    }
                }
            }
            finally
            {
                m_EvictionCandidates.Clear();
                m_IsEvictingCache = false;
            }
            ExceptionUtility.ThrowIfAny(failures);
        }

        /// <summary>
        ///   <para>比较关闭页面的缓存顺序。</para>
        /// </summary>
        /// <param name="left">左。</param>
        /// <param name="right">右。</param>
        private static int CompareKeepAliveEntries(ViewEntry left, ViewEntry right)
        {
            var order = left.LastAccessOrder.CompareTo(right.LastAccessOrder);
            return order != 0
                ? order
                : left.InstanceId.CompareTo(right.InstanceId);
        }
    }
}

#endif
