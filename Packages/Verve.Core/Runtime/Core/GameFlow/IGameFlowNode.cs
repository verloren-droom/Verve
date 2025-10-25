namespace Verve
{
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    ///  <para>游戏流程节点接口</para>
    /// </summary>
    public interface IGameFlowNode
    {
        /// <summary>
        ///  <para>节点ID</para>
        /// </summary>
        string NodeID { get; }
        
        /// <summary>
        ///  <para>是否完成</para>
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        ///  <para>是否正在执行</para>
        /// </summary>
        bool IsExecuting { get; }
    
        /// <summary>
        ///  <para>开始执行</para>
        /// </summary>
        Task Execute(CancellationToken ct = default);
        
        /// <summary>
        ///  <para>取消执行</para>
        /// </summary>
        void Cancel();
        
        /// <summary>
        ///  <para>重置</para>
        /// </summary>
        void Reset();
    }
}