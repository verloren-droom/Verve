namespace Verve.AI
{
    using System.Runtime.CompilerServices;
    
    
    public static class BTNodeExtensions
    {
        /// <summary>
        /// 执行子节点（自动处理准备逻辑）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NodeStatus RunChildNode(this ICompositeNode _, ref IBTNode child, ref NodeRunContext ctx)
        {
            if (child is IPreparableNode preparable)
            {
                preparable.Prepare(ref ctx);
            }
            return child?.Run(ref ctx) ?? NodeStatus.Failure;
        }
    }
}