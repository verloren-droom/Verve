namespace Verve.Timer
{
    using System;
    using System.Collections.Generic;
    
    
    public abstract class TimerServiceBase : ITimerService
    {
        protected readonly List<TimerData> m_Timers = new List<TimerData>();
        
        public float TimeScale { get; set; } = 1.0f;
        public float ElapsedTime { get; protected set;}
        public bool IsRunning { get; set; } = true;
        
        
        public virtual int AddTimer(float duration, Action onComplete, bool loop = false)
        {
            var timer = new TimerData(ElapsedTime + duration, onComplete, loop);
            m_Timers.Add(timer);
            return timer.ID;
        }
        
        public virtual void RemoveTimer(int id)
        {
            m_Timers.RemoveAll(x => x.ID == id);
        }

        public virtual void RemoveTimer(Action onComplete)
        {
            m_Timers.RemoveAll(x => x.OnComplete == onComplete);
        }

        public virtual void ClearTimer()
        {
            m_Timers.Clear();
        }
        
        public virtual void Update(float deltaTime)
        {
            if (!IsRunning) return;
            ElapsedTime += deltaTime * TimeScale;

            var i = 0;
            while (i < m_Timers.Count)
            {
                if (m_Timers[i].Duration > ElapsedTime) break;

                TimerData timer = m_Timers[i];
                var action = timer.OnComplete;
                m_Timers.RemoveAt(i);
                try { action?.Invoke(); }
                catch { }
            }
        }
    }
}