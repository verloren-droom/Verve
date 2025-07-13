namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    /// 计时器子模块基类
    /// </summary>
    public abstract class TimerSubmodule : ITimerSubmodule
    {
        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }
        public virtual void OnModuleUnloaded() { }

        public float TimeScale { get; set; } = 1.0f;
        public float ElapsedTime { get; protected set;}
        public bool IsRunning { get; set; }


        public abstract int AddTimer(float duration, Action onComplete, bool loop = false);

        public abstract void RemoveTimer(int id);

        public abstract void RemoveTimer(Action onComplete);

        public abstract void ClearTimer();

        public abstract void Update(float deltaTime);
    }
}