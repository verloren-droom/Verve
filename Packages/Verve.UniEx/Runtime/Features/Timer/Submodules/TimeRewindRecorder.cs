#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Timer
{
    using System;
    using Verve.Timer;
    using UnityEngine;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>时间回溯记录器</para>
    /// </summary>
    [Serializable]
    internal class TimeRewindRecorder : IDisposable
    {
        /// <summary>
        ///   <para>快照数据</para>
        /// </summary>
        private readonly RewindSnapshot[] m_Buffer;
        private readonly int m_Capacity;
        private int m_WriteIndex;
        private int m_Count;
        
        private bool m_IsRewinding;
        private int m_LastAppliedIndex = -1;
        
        /// <summary>
        ///   <para>目标可回溯对象</para>
        /// </summary>
        public ITimeRewindable Target { get; }
        /// <summary>
        ///   <para>当前快照数量</para>
        /// </summary>
        public int SnapshotCount => m_Count;
        /// <summary>
        ///   <para>快照缓冲区容量</para>
        /// </summary>
        public int BufferCapacity => m_Capacity;
        
        /// <summary>
        ///   <para>最早的快照时间戳</para>
        /// </summary>
        public float OldestTimestamp => m_Count > 0 ? GetSnapshotAt(0).timestamp : 0f;
        /// <summary>
        ///   <para>最新快照时间戳</para>
        /// </summary>
        public float NewestTimestamp => m_Count > 0 ? GetSnapshotAt(m_Count - 1).timestamp : 0f;

        
        public TimeRewindRecorder(ITimeRewindable target, int maxCapacity)
        {
            Target = target;
            m_Capacity = Math.Max(2, maxCapacity);
            m_Buffer = new RewindSnapshot[m_Capacity];
            m_WriteIndex = 0;
            m_Count = 0;
            
            target.OnStateChanged += OnTargetStateChanged;
            
            RecordSnapshot(Time.unscaledTime);
        }
        
        private void OnTargetStateChanged(ITimeRewindable target)
        {
            if (!Target.CanRewind || m_IsRewinding) return;
            RecordSnapshot(Time.unscaledTime);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordSnapshot(float timestamp)
        {
            var newSnapshot = Target.CaptureSnapshot();
            if (newSnapshot == null) return;
            
            if (m_Count > 0)
            {
                var lastSnapshot = GetSnapshotAt(m_Count - 1);
                if (Target.CompareSnapshot(lastSnapshot.snapshot, newSnapshot))
                {
                    return;
                }
            }
            
            m_Buffer[m_WriteIndex] = new RewindSnapshot(newSnapshot, timestamp, Time.frameCount);
            m_WriteIndex = (m_WriteIndex + 1) % m_Capacity;
            
            if (m_Count < m_Capacity)
            {
                m_Count++;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private RewindSnapshot GetSnapshotAt(int logicalIndex)
        {
            if (logicalIndex < 0 || logicalIndex >= m_Count)
                throw new ArgumentOutOfRangeException(nameof(logicalIndex));
            
            int physicalIndex;
            if (m_Count < m_Capacity)
            {
                physicalIndex = logicalIndex;
            }
            else
            {
                physicalIndex = (m_WriteIndex + logicalIndex) % m_Capacity;
            }
            
            return m_Buffer[physicalIndex];
        }
        
        /// <summary>
        ///   <para>开始回溯</para>
        /// </summary>
        /// <param name="startTime">开始时间</param>
        public void StartRewind(float startTime)
        {
            m_IsRewinding = true;
            
            m_LastAppliedIndex = FindSnapshotIndex(startTime);
            if (m_LastAppliedIndex < 0)
            {
                m_LastAppliedIndex = m_Count - 1;
            }
        }
        
        /// <summary>
        ///   <para>停止回溯</para>
        /// </summary>
        public void StopRewind()
        {
            m_IsRewinding = false;
            m_LastAppliedIndex = -1;
        }
        
        /// <summary>
        ///   <para>根据时间查找快照索引</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindSnapshotIndex(float targetTime)
        {
            if (m_Count == 0) return -1;
            
            if (targetTime <= OldestTimestamp) return 0;
            if (targetTime >= NewestTimestamp) return m_Count - 1;
            
            int left = 0;
            int right = m_Count - 1;
            int bestIndex = -1;
            
            while (left <= right)
            {
                int mid = (left + right) / 2;
                var snapshot = GetSnapshotAt(mid);
                
                if (snapshot.timestamp <= targetTime)
                {
                    bestIndex = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
            
            return bestIndex;
        }
        
        /// <summary>
        /// 根据时间获取应该显示的快照
        /// </summary>
        public RewindSnapshot? GetSnapshotAtTime(float targetTime)
        {
            if (!m_IsRewinding || m_Count == 0) return null;
            
            int targetIndex = FindSnapshotIndex(targetTime);
            if (targetIndex < 0) return null;
            
            if (targetIndex != m_LastAppliedIndex)
            {
                m_LastAppliedIndex = targetIndex;
                return GetSnapshotAt(targetIndex);
            }
            
            return null;
        }
        
        /// <summary>
        ///   <para>检查是否已经倒放到最早状态</para>
        /// </summary>
        /// <returns>
        ///   <para>是否已经倒放到最早状态</para>
        /// </returns>
        public bool HasReachedOldest()
        {
            return m_LastAppliedIndex == 0;
        }
        
        /// <summary>
        ///   <para>清空快照</para>
        /// </summary>
        public void Clear()
        {
            m_Count = 0;
            m_WriteIndex = 0;
            m_LastAppliedIndex = -1;
            m_IsRewinding = false;
            Array.Clear(m_Buffer, 0, m_Buffer.Length);
        }
        
        public void Dispose()
        {
            if (Target != null)
            {
                Target.OnStateChanged -= OnTargetStateChanged;
            }
            Clear();
        }
    }
}

#endif