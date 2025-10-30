namespace Verve.AI
{
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>复合节点扩展</para>
    /// </summary>
    public static class CompositeBTNodeExtensions
    {
        /// <summary>
        ///   <para>执行子节点（自动处理准备逻辑）</para>
        /// </summary>
        /// <param name="child">子节点</param>
        /// <param name="ctx">节点运行上下文</param>
        /// <returns>
        ///   <para>节点结果</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BTNodeResult RunChildNode(this ICompositeBTNode _, ref IBTNode child, ref BTNodeRunContext ctx)
        {
            (child as IBTNodePreparable)?.Prepare(ref ctx);
            return child?.Run(ref ctx) ?? BTNodeResult.Failed;
        }

        /// <summary>
        ///   <para>重置子节点</para>
        /// </summary>
        /// <param name="child">子节点</param>
        /// <param name="ctx">节点重置上下文</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetChildNode(this ICompositeBTNode _, ref IBTNode child, ref BTNodeResetContext ctx)
        {
            (child as IBTNodeResettable)?.Reset(ref ctx);
        }
        
        /// <summary>
        ///   <para>重置所有子节点</para>
        /// </summary>
        /// <param name="ctx">节点重置上下文</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetChildrenNode(this ICompositeBTNode self, ref BTNodeResetContext ctx)
        {
            foreach (var child in self.GetChildren())
            {
                (child as IBTNodeResettable)?.Reset(ref ctx);
            }
        }
        
        /// <summary>
        ///   <para>重置所有激活的子节点</para>
        /// </summary>
        /// <param name="ctx">节点重置上下文</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetActiveChildrenNode(this ICompositeBTNode self, ref BTNodeResetContext ctx)
        {
            foreach (var child in self.GetActiveChildren())
            {
                (child as IBTNodeResettable)?.Reset(ref ctx);
            }
        }
    }
}