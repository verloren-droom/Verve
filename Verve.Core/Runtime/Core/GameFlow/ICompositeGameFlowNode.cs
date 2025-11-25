namespace Verve
{
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>组合游戏流程节点</para>
    /// </summary>
    public interface ICompositeGameFlowNode : IGameFlowNode
    {
        /// <summary>
        ///   <para>子节点</para>
        /// </summary>
        IEnumerable<IGameFlowNode> Children { get; }
        
        /// <summary>
        ///   <para>添加子节点</para>
        /// </summary>
        /// <param name="node">子节点</param>
        void Append(IGameFlowNode node);
        
        /// <summary>
        ///   <para>插入子节点到指定位置</para>
        /// </summary>
        /// <param name="node">子节点</param>
        /// <param name="index">索引</param>
        void Insert(IGameFlowNode node, int index);
        
        /// <summary>
        ///  <para>删除子节点</para>
        /// </summary> 
        /// <param name="node">子节点</param>
        void Remove(IGameFlowNode node);
        
        /// <summary>
        ///   <para>删除子节点</para>
        /// </summary>
        /// <param name="index"></param>
        void RemoveAt(int index);
        
        /// <summary>
        ///   <para>清空所有子节点</para>
        /// </summary>
        void Clear();
    }
}