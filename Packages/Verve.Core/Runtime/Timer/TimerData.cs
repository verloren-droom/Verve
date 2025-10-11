namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    /// 计时器数据
    /// </summary>
    [Serializable]
    public readonly struct TimerData : IComparable<TimerData>
    {
        /// <summary> 计时器ID </summary>
        public readonly int id;
        /// <summary> 持续时间 </summary>
        public readonly float duration;
        /// <summary> 计时器完成时的回调 </summary>
        public readonly Action onComplete;
        /// <summary> 是否循环计时 </summary>
        public readonly bool isLooping;

        private static int m_ID;
        
        public TimerData(float duration, Action onComplete, bool isLooping = false)
        {
            this.duration = Math.Max(duration, 0);
            this.onComplete = onComplete;
            this.isLooping = isLooping;
            id = m_ID++;
        }

        public int CompareTo(TimerData other)
        {
            return id.CompareTo(other.id);
        }
    }
}