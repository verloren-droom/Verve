namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    /// 计时器子模块接口
    /// </summary>
    public interface ITimer
    {
        /// <summary> 时间缩放 </summary>
        float TimeScale { get; set; }
        /// <summary> 是否运行中 </summary>
        public bool IsRunning { get; set; }
        /// <summary>
        /// 添加计时器
        /// </summary>
        int AddTimer(float duration, Action onComplete, bool loop = false);
        /// <summary>
        /// 移除计时器
        /// </summary>
        void RemoveTimer(int id);
        /// <summary>
        /// 移除计时器
        /// </summary>
        void RemoveTimer(Action onComplete);
        /// <summary>
        /// 清空计时器
        /// </summary>
        void ClearTimer();
    }
}