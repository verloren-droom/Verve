namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    /// 计时器数据
    /// </summary>
    [Serializable]
    public readonly struct TimerData : IComparable<TimerData>
    {
        public readonly int ID;
        /// <summary>
        /// 持续时间
        /// </summary>
        public readonly float Duration;

        /// <summary>
        /// 计时器完成时的回调
        /// </summary>
        public readonly Action OnComplete;

        /// <summary>
        /// 是否循环计时
        /// </summary>
        public readonly bool IsLooping;

        private static int m_ID;
        
        public TimerData(float duration, Action onComplete, bool isLooping = false)
        {
            Duration = Math.Max(duration, 0);
            OnComplete = onComplete;
            IsLooping = isLooping;
            ID = m_ID++;
        }

        public int CompareTo(TimerData other)
        {
            return ID.CompareTo(other.ID);
        }
    }
}