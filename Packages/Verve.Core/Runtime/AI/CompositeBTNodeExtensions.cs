namespace Verve.AI
{
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 复合节点扩展
    /// </summary>
    public static class CompositeBTNodeExtensions
    {
        /// <summary>
        /// 执行子节点（自动处理准备逻辑）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BTNodeResult RunChildNode(this ICompositeBTNode _, ref IBTNode child, ref BTNodeRunContext ctx)
        {
            (child as IBTNodePreparable)?.Prepare(ref ctx);
            return child?.Run(ref ctx) ?? BTNodeResult.Failed;
        }

        /// <summary>
        /// 重置子节点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetChildNode(this ICompositeBTNode _, ref IBTNode child, ref BTNodeResetContext ctx, bool recursive = false)
        {
            (child as IBTNodeResettable)?.Reset(ref ctx);
            if(recursive)
            {
                (child as ICompositeBTNode)?.ResetChildrenNode(ref ctx, recursive);
            }
        }
        
        /// <summary>
        /// 重置所有子节点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetChildrenNode(this ICompositeBTNode self, ref BTNodeResetContext ctx, bool recursive = false)
        {
            foreach (var child in self.GetChildren())
            {
                (child as IBTNodeResettable)?.Reset(ref ctx);
                if (recursive && child is ICompositeBTNode composite)
                {
                    composite.ResetChildrenNode(ref ctx, true);
                }
            }
        }
        
        /// <summary>
        /// 重置所有激活的子节点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetActiveChildrenNode(this ICompositeBTNode self, ref BTNodeResetContext ctx, bool recursive = false)
        {
            foreach (var child in self.GetActiveChildren())
            {
                (child as IBTNodeResettable)?.Reset(ref ctx);
                if (recursive && child is ICompositeBTNode composite)
                {
                    composite.ResetActiveChildrenNode(ref ctx, true);
                }
            }
        }
    }
}