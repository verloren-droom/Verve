namespace Verve.Timer
{
    using System;

    
    public class SimpleTimerService : TimerServiceBase
    {
        public override int AddTimer(float duration, Action onComplete, bool loop = false)
        {
            var timer = new TimerData(ElapsedTime + duration, onComplete, loop);

            int index = m_Timers.BinarySearch(timer);
            if (index < 0) index = ~index;

            m_Timers.Insert(index, timer);
            return timer.ID;
        }
    }
}