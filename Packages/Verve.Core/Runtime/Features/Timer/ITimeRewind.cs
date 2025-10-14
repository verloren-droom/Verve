namespace Verve.Timer
{
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 时间回溯接口
    /// </summary>
    public interface ITimeRewind
    {
        /// <summary> 可回溯对象集合 </summary>
        IReadOnlyCollection<ITimeRewindable> Rewindables { get; }
        /// <summary> 是否正在回溯 </summary>
        bool IsRewinding { get; }
        /// <summary>
        /// 回溯到指定时间点
        /// </summary>
        /// <param name="targetTime">目标时间戳</param>
        /// <param name="speedMultiplier">倒放速度（0表示即时跳转）</param>
        /// <returns></returns>
        void RewindToTime(float targetTime, float speedMultiplier = 1.0f);
        /// <summary> 结束回溯 </summary>
        void StopRewind();
    }
}