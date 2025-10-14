namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    /// 可时间回溯接口
    /// </summary>
    public interface ITimeRewindable
    {
        /// <summary> 是否可以被回溯 </summary>
        bool CanRewind { get; }
        
        /// <summary> 当状态发生变化时触发 </summary>
        event Action<ITimeRewindable> OnStateChanged;
        
        /// <summary>
        /// 捕获快照
        /// </summary>
        /// <returns>快照数据</returns>
        object CaptureSnapshot();
        
        /// <summary>
        /// 恢复快照
        /// </summary>
        /// <param name="snapshot">之前捕获的快照数据</param>
        void RestoreSnapshot(object snapshot);
        
        /// <summary>
        /// 比较状态数据是否相等
        /// </summary>
        bool CompareSnapshot(object snapshot1, object snapshot2);
    }
}