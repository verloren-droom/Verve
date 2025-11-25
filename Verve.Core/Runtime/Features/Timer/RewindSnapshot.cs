namespace Verve.Timer
{
    using System;

    
    /// <summary>
    ///   <para>时间回溯快照</para>
    /// </summary>
    [Serializable]
    public readonly struct RewindSnapshot : IEquatable<RewindSnapshot>
    {
        /// <summary>
        ///   <para>时间回溯快照数据</para>
        /// </summary>
        public readonly object snapshot;
        /// <summary>
        ///   <para>时间戳</para>
        /// </summary>
        public readonly float timestamp;
        /// <summary>
        ///   <para>帧数</para>
        /// </summary>
        public readonly int frame;
        /// <summary>
        ///   <para>快照是否为关键帧</para>
        /// </summary>
        public readonly bool keyFrame;
        
        public RewindSnapshot(object snapshot, float timestamp, int frame, bool keyFrame = true)
        {
            this.snapshot = snapshot;
            this.timestamp = timestamp;
            this.frame = frame;
            this.keyFrame = keyFrame;
        }

        public bool Equals(RewindSnapshot other)
        {
            if (snapshot == null && other.snapshot == null) return true;
            if (snapshot == null || other.snapshot == null) return false;
            return snapshot.Equals(other.snapshot);
        }
        
        public override bool Equals(object obj)
        {
            return obj is RewindSnapshot other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ frame;
                hashCode = (hashCode * 397) ^ (snapshot?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
        
        public override string ToString()
        {
            return $"RewindSnapshot(t: {timestamp:F2}, frame: {frame}, key: {keyFrame}, data: {snapshot})";
        }
    }
}