namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>资源清理工具；统一释放集合中的资源并汇总错误。</para>
    /// </summary>
    internal static class ResourceUtility
    {
        /// <summary>
        ///   <para>逆序移除并释放全部资源；清理完毕后抛出错误。</para>
        /// </summary>
        /// <param name="resources">待清空的资源列表。</param>
        /// <param name="release">单个资源的释放操作。</param>
        /// <typeparam name="T">资源类型。</typeparam>
        public static void ReleaseAll<T>(List<T> resources, Action<T> release)
        {
            List<Exception> errors = null;
            while (resources.Count > 0)
            {
                int index = resources.Count - 1;
                var resource = resources[index];
                resources.RemoveAt(index);
                try { release(resource); }
                catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            }
            ExceptionUtility.ThrowIfAny(errors);
        }
    }
}