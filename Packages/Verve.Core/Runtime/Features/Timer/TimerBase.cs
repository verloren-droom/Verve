namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    ///   <para>计时器基类</para>
    /// </summary>
    public abstract class TimerBase : ITimer
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