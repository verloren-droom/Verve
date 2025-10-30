namespace Verve.Timer
{
    using System;
    
    
    /// <summary>
    ///   <para>计时器数据</para>
    /// </summary>
    [Serializable]
    public readonly struct TimerData : IComparable<TimerData>
    {
        /// <summary>
        ///   <para>计时器ID</para>
        /// </summary>
        public readonly int id;
        /// <summary>
        ///   <para>计时器持续时间</para>
        /// </summary>
        public readonly float duration;
        /// <summary>
        ///   <para>计时器完成时的回调</para>
        /// </summary>
        public readonly Action onComplete;
        /// <summary>
        ///   <para>计时器是否循环</para>
        /// </summary>
        public readonly bool loop;
        
        
        /// <summary>
        ///   <para>创建计时器数据</para>
        /// </summary>
        /// <param name="duration">计时器持续时间</param>
        /// <param name="onComplete">计时器完成时的回调</param>
        /// <param name="loop">计时器是否循环</param>
        public TimerData(float duration, Action onComplete, bool loop = false)
        {
            this.duration = Math.Max(duration, 0);
            this.onComplete = onComplete;
            this.loop = loop;
            this.id = Guid.NewGuid().GetHashCode();
        }

        public int CompareTo(TimerData other)
        {
            return id.CompareTo(other.id);
        }
    }
}