#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections;
    using System.Threading;
    using UnityEngine;

    /// <summary>
    ///   <para>补间工具；按时间生成 0～1 进度。</para>
    /// </summary>
    public static class Tween
    {
        /// <summary>
        ///   <para>创建进度协程；零时长直接应用终点，取消与回调错误直接抛出。</para>
        /// </summary>
        /// <param name="duration">持续时间（秒）。</param>
        /// <param name="apply">进度回调；参数范围为 0～1。</param>
        /// <param name="unscaledTime">是否使用不受缩放影响的时间。</param>
        /// <param name="ct">取消令牌。</param>
        public static IEnumerator Run(float duration, Action<float> apply, bool unscaledTime = false,
            CancellationToken ct = default)
        {
            if (duration < 0 || float.IsNaN(duration) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return Animate(duration, apply, unscaledTime, ct);
        }

        /// <summary>
        ///   <para>执行动画。</para>
        /// </summary>
        /// <param name="duration">持续时间（秒）。</param>
        /// <param name="apply">进度回调；参数范围为 0～1。</param>
        /// <param name="unscaledTime">是否使用不受缩放影响的时间。</param>
        /// <param name="ct">取消令牌。</param>
        private static IEnumerator Animate(float duration, Action<float> apply, bool unscaledTime,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (duration == 0)
            {
                apply(1f);
                yield break;
            }

            apply(0f);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                yield return null;
                ct.ThrowIfCancellationRequested();
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                apply(Mathf.Min(elapsed / duration, 1f));
            }
        }
    }
}

#endif