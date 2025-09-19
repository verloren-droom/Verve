#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Timer
{
    using System;
    using Verve.Timer;
    
    
    /// <summary>
    /// 计算器子模块基类
    /// </summary>
    public abstract class TimerSubmodule : TickableGameFeatureSubmodule, ITimer
    {
        public float TimeScale { get; set; } = 1.0f;
        public float ElapsedTime { get; protected set;}
        public bool IsRunning { get; set; }


        public abstract int AddTimer(float duration, Action onComplete, bool loop = false);
        public abstract void RemoveTimer(int id);
        public abstract void RemoveTimer(Action onComplete);
        public abstract void ClearTimer();
    }
}

#endif