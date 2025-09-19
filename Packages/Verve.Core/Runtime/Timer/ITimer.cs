namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    /// 计时器子模块接口
    /// </summary>
    public interface ITimer
    {
        float TimeScale { get; set; }
        public bool IsRunning { get; set; }
        int AddTimer(float duration, Action onComplete, bool loop = false);
        void RemoveTimer(int id);
        void RemoveTimer(Action onComplete);
        void ClearTimer();
    }
}