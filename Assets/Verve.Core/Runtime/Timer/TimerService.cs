namespace Verve.Timer
{
    using System;
    
    
    public abstract class TimerServiceBase : ITimerService
    {
        public float TimeScale { get; set; } = 1.0f;
        public float ElapsedTime { get; protected set;}
        public bool IsRunning { get; set; } = true;


        public abstract int AddTimer(float duration, Action onComplete, bool loop = false);

        public abstract void RemoveTimer(int id);

        public abstract void RemoveTimer(Action onComplete);

        public abstract void ClearTimer();

        public abstract void Update(float deltaTime);
    }
}