namespace Verve.Timer
{
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>时间回溯接口</para>
    /// </summary>
    public interface ITimeRewind
    {
        /// <summary>
        ///   <para>可回溯对象集合</para>
        /// </summary>
        IReadOnlyCollection<ITimeRewindable> Rewindables { get; }
        
        /// <summary>
        ///   <para>是否正在回溯</para>
        /// </summary>
        bool IsRewinding { get; }
        
        /// <summary>
        ///   <para>回溯到指定时间点</para>
        /// </summary>
        /// <param name="targetTime">目标时间戳</param>
        /// <param name="speedMultiplier">倒放速度（0表示即时跳转）</param>
        void RewindToTime(float targetTime, float speedMultiplier = 1.0f);
        
        /// <summary>
        ///   <para>停止回溯</para>
        /// </summary>
        void StopRewind();
    }
}