namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    ///   <para>可时间回溯接口</para>
    /// </summary>
    public interface ITimeRewindable
    {
        /// <summary>
        ///   <para>是否可以被回溯</para>
        /// </summary>
        bool CanRewind { get; }
        
        /// <summary>
        ///   <para>当状态发生变化时触发</para>
        /// </summary>
        event Action<ITimeRewindable> OnStateChanged;
        
        /// <summary>
        ///   <para>捕获快照</para>
        /// </summary>
        /// <returns>
        ///   <para>快照数据</para>
        /// </returns>
        object CaptureSnapshot();
        
        /// <summary>
        ///   <para>恢复快照</para>
        /// </summary>
        /// <param name="snapshot">之前捕获的快照数据</param>
        void RestoreSnapshot(object snapshot);
        
        /// <summary>
        ///   <para>比较状态数据是否相等</para>
        /// </summary>
        /// <param name="snapshot1">状态数据1</param>
        /// <param name="snapshot2">状态数据2</param>
        /// <returns>
        ///   <para>是否相等</para>
        /// </returns>
        bool CompareSnapshot(object snapshot1, object snapshot2);
    }
}