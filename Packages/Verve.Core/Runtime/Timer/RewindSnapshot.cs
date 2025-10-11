namespace Verve.Timer
{
    using System;

    
    /// <summary>
    /// 时间回溯快照
    /// </summary>
    [Serializable]
    public readonly struct RewindSnapshot : IEquatable<RewindSnapshot>
    {
        public readonly object snapshot;
        public readonly float timestamp;
        public readonly int frame;
        public readonly bool isKeyFrame;
        
        public RewindSnapshot(object snapshot, float timestamp, int frame, bool isKeyFrame = true)
        {
            this.snapshot = snapshot;
            this.timestamp = timestamp;
            this.frame = frame;
            this.isKeyFrame = isKeyFrame;
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
            return $"RewindSnapshot(t: {timestamp:F2}, frame: {frame}, key: {isKeyFrame}, data: {snapshot})";
        }
    }
}