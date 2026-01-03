#if UNITY_5_3_OR_NEWER

namespace Verve.Timer
{
    using System;
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>简单的计时器</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(TimerGameFeature), Description = "简单计时器")]
    public sealed class SimpleTimer : TimerSubmodule
    {
        private readonly List<TimerData> m_Timers = new List<TimerData>();

        
        public override int AddTimer(float duration, Action onComplete, bool loop = false)
        {
            var timer = new TimerData(ElapsedTime + duration, onComplete, loop);

            int index = m_Timers.BinarySearch(timer);
            if (index < 0) index = ~index;

            m_Timers.Insert(index, timer);
            return timer.id;
        }
        
        public override void RemoveTimer(int id)
        {
            m_Timers.RemoveAll(x => x.id == id);
        }
        
        public override void RemoveTimer(Action onComplete)
        {
            m_Timers.RemoveAll(x => x.onComplete == onComplete);
        }

        protected override void OnTick(in GameFeatureContext ctx)
        {
            base.OnTick(in ctx);
            if (!IsRunning) return;
            ElapsedTime += ctx.DeltaTime * TimeScale;

            var i = 0;
            while (i < m_Timers.Count)
            {
                if (m_Timers[i].duration > ElapsedTime) break;

                TimerData timer = m_Timers[i];
                var action = timer.onComplete;
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

#endif