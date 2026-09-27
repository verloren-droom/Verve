namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;

    /// <summary>
    ///   <para>异常工具；汇总清理错误，保留单个异常的调用栈。</para>
    /// </summary>
    internal static class ExceptionUtility
    {
        /// <summary>
        ///   <para>收集错误。</para>
        /// </summary>
        /// <param name="errors">错误收集列表。</param>
        /// <param name="error">错误。</param>
        internal static void Add(ref List<Exception> errors, Exception error)
        {
            if (error == null) return;
            errors ??= new List<Exception>();
            if (error is AggregateException aggregate)
                errors.AddRange(aggregate.Flatten().InnerExceptions);
            else
                errors.Add(error);
        }

        /// <summary>
        ///   <para>合并错误。</para>
        /// </summary>
        /// <param name="primary">主要错误。</param>
        /// <param name="secondary">附加错误。</param>
        internal static Exception Combine(Exception primary, Exception secondary)
        {
            if (primary == null) return secondary;
            if (secondary == null) return primary;
            List<Exception> errors = null;
            Add(ref errors, primary);
            Add(ref errors, secondary);
            return Combine(errors);
        }

        /// <summary>
        ///   <para>合并错误。</para>
        /// </summary>
        /// <param name="errors">已收集的错误。</param>
        /// <param name="message">错误信息。</param>
        internal static Exception Combine(List<Exception> errors, string message = null)
        {
            if (errors == null || errors.Count == 0) return null;
            return errors.Count == 1 ? errors[0] : new AggregateException(message, errors);
        }

        /// <summary>
        ///   <para>存在错误时抛出异常。</para>
        /// </summary>
        /// <param name="errors">已收集的错误。</param>
        /// <param name="message">错误信息。</param>
        internal static void ThrowIfAny(List<Exception> errors, string message = null) => Rethrow(Combine(errors, message));

        /// <summary>
        ///   <para>重新抛出异常并保留调用栈。</para>
        /// </summary>
        /// <param name="error">错误。</param>
        internal static void Rethrow(Exception error)
        {
            if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}