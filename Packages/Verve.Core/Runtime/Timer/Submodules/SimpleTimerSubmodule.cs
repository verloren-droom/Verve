namespace Verve.Timer
{
    using System;
    using System.Collections.Generic;

    
    /// <summary>
    /// 简单的计时器子模块
    /// </summary>
    [Serializable]
    public class SimpleTimerSubmodule : TimerSubmodule
    {
        public override string ModuleName => "SimpleTimer";
        
        private readonly List<TimerData> m_Timers = new List<TimerData>();

        
        public override int AddTimer(float duration, Action onComplete, bool loop = false)
        {
            var timer = new TimerData(ElapsedTime + duration, onComplete, loop);

            int index = m_Timers.BinarySearch(timer);
            if (index < 0) index = ~index;

            m_Timers.Insert(index, timer);
            return timer.ID;
        }
        
        public override void RemoveTimer(int id)
        {
            m_Timers.RemoveAll(x => x.ID == id);
        }
        
        public override void RemoveTimer(Action onComplete)
        {
            m_Timers.RemoveAll(x => x.OnComplete == onComplete);
        }
        
        public override void Update(float deltaTime)
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
        
        public override void ClearTimer()
        {
            m_Timers.Clear();
        }
    }
}