namespace Verve.Timer
{
    using System;
    using UnityEngine;
    using System.Threading;
    
    public class TimerHandle : IComparable<TimerHandle>
    {
        public int TimeId { get; }
        public float TriggerTime { get; }
        
        private event Action m_OnTimeout;
        
        public float Interval { get; }
        
        public bool IsLooping => Interval > 0;

        private CancellationTokenSource m_CancellationTokenSource;
        
        private static int m_IDCounter;

        public int CompareTo(TimerHandle other)
        {
            return TriggerTime.CompareTo(other.TriggerTime);
        }

        public TimerHandle(Action onTimeout)
        {
            TimeId = Interlocked.Increment(ref m_IDCounter);
            m_OnTimeout = onTimeout;
            m_CancellationTokenSource = new CancellationTokenSource();
        }

        public async void Start(bool ignoreTimeScale = false)
        {
// #if VERVE_UNIEX_0_0_1_OR_NEWER
//             if (ignoreTimeScale)
//             {
//                 await new WaitForSecondsRealtime(TriggerTime);
//             }
//             else
//             {
//                 await new WaitForSeconds(TriggerTime);
//             }
// #endif
            m_OnTimeout?.Invoke();
        }

        public void Stop()
        {
            try
            {
                m_CancellationTokenSource?.Cancel();
                m_CancellationTokenSource?.Dispose();
            }
            catch { }
        }
    }
}