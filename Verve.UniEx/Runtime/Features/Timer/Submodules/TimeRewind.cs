#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Timer
{
    using UniEx;
    using System;
    using System.Linq;
    using UnityEngine;
    using Verve.Timer;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>时间回溯子模块</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(TimerGameFeature), Description = "时间回溯子模块")]
    public sealed partial class TimeRewind : TickableGameFeatureSubmodule<TimerGameFeatureComponent>, ITimeRewind
    {
        private readonly Dictionary<ITimeRewindable, TimeRewindRecorder> m_Recorders = 
            new Dictionary<ITimeRewindable, TimeRewindRecorder>();
        private readonly HashSet<ITimeRewindable> m_ActiveRewindables = new HashSet<ITimeRewindable>();
        
        private bool m_IsRewinding;
        private float m_RewindSpeed = 1.0f;
        private float m_RecordingEndTime;
        private float m_TargetRewindTime;
        
        public IReadOnlyCollection<ITimeRewindable> Rewindables => m_ActiveRewindables;
        public bool IsRewinding => m_IsRewinding;
        public float CurrentRewindTime { get; private set; }
        public int TotalRecordedObjects => m_Recorders.Count;

        
        protected override IEnumerator OnStartup()
        {
            yield return null;
            CleanupInvalidRewindables();
        }

        protected override void OnTick(in GameFeatureContext ctx)
        {
            if (m_IsRewinding && Application.isPlaying)
            {
                UpdateRewind(Time.unscaledDeltaTime);
            }
        }

        protected override void OnShutdown()
        {
            // ClearAllRecorders();
        }

        /// <summary>
        ///   <para>定点回溯</para>
        /// </summary>
        /// <param name="targetTime">目标时间</param>
        /// <param name="speedMultiplier">回溯速度</param>
        public void RewindToTime(float targetTime, float speedMultiplier = 1.0f)
        {
            if (m_IsRewinding || m_Recorders.Count == 0) { return; }

            m_RecordingEndTime = m_Recorders.Values.Max(r => r.NewestTimestamp);
    
            float oldestTime = m_Recorders.Values.Min(r => r.OldestTimestamp);
            float clampedTargetTime = Mathf.Clamp(targetTime, oldestTime, m_RecordingEndTime);

            
            m_IsRewinding = true;
            m_RewindSpeed = Mathf.Max(0.1f, speedMultiplier);
    
            m_RecordingEndTime = m_Recorders.Values.Max(r => r.NewestTimestamp);
            CurrentRewindTime = m_RecordingEndTime;
    
            m_TargetRewindTime = clampedTargetTime;
    
            foreach (var recorder in m_Recorders.Values)
            {
                recorder.StartRewind(m_RecordingEndTime);
            }
    
            Debug.Log($"[TimeRewind] 开始定点回溯: 从 {CurrentRewindTime:F2} 到 {m_TargetRewindTime:F2}, 速度: {m_RewindSpeed}x");
        } 
        
        /// <summary>
        ///   <para>停止回溯</para>
        /// </summary>
        public void StopRewind()
        {
            if (!m_IsRewinding) return;
            m_IsRewinding = false;
            
            foreach (var recorder in m_Recorders.Values)
            {
                recorder.StopRewind();
            }
            
            m_TargetRewindTime = 0f;
        }

        /// <summary>
        ///   <para>添加倒流对象</para>
        /// </summary>
        /// <param name="rewindable">倒流对象</param>
        public void AddRewindable(ITimeRewindable rewindable)
        {
            if (rewindable == null || m_Recorders.ContainsKey(rewindable)) return;
            
            int maxCapacity = Mathf.CeilToInt(Component.MaxRewindTime * 30);
            var recorder = new TimeRewindRecorder(rewindable, maxCapacity);
            
            m_Recorders.Add(rewindable, recorder);
            m_ActiveRewindables.Add(rewindable);
        }

        /// <summary>
        ///   <para>移除倒流对象</para>
        /// </summary>
        /// <param name="rewindable">倒流对象</param>
        public void RemoveRewindable(ITimeRewindable rewindable)
        {
            if (rewindable == null) return;
            
            if (m_Recorders.TryGetValue(rewindable, out var recorder))
            {
                recorder.Dispose();
                m_Recorders.Remove(rewindable);
            }
            
            m_ActiveRewindables.Remove(rewindable);
        }
        
        /// <summary>
        ///   <para>清空所有记录器</para>
        /// </summary>
        public void ClearAllRecorders()
        {
            foreach (var recorder in m_Recorders.Values)
            {
                recorder.Dispose();
            }
            m_Recorders.Clear();
            m_ActiveRewindables.Clear();
        }

        /// <summary>
        ///   <para>清理无效的倒流对象</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CleanupInvalidRewindables()
        {
            var invalidRewindables = m_ActiveRewindables.Where(r => r == null).ToList();
            foreach (var invalid in invalidRewindables)
            {
                RemoveRewindable(invalid);
            }
        }
        
        /// <summary>
        ///   <para>更新回溯状态</para>
        /// </summary>
        /// <param name="deltaTime">时间间隔</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateRewind(float deltaTime)
        {
            float rewindDelta = deltaTime * m_RewindSpeed;
            CurrentRewindTime -= rewindDelta;
            
            if (m_TargetRewindTime > 0 && CurrentRewindTime <= m_TargetRewindTime)
            {
                CurrentRewindTime = m_TargetRewindTime;
                ApplyRewindForTime(CurrentRewindTime);
                StopRewind();
                Debug.Log($"[TimeRewind] 到达目标时间: {m_TargetRewindTime:F2}");
                return;
            }
            
            int restoredCount = 0;
            int reachedOldestCount = 0;
            
            foreach (var kvp in m_Recorders)
            {
                var rewindable = kvp.Key;
                var recorder = kvp.Value;
                
                if (rewindable == null || !rewindable.CanRewind) continue;
                
                if (recorder.HasReachedOldest())
                {
                    reachedOldestCount++;
                    continue;
                }
                
                var snapshot = recorder.GetSnapshotAtTime(CurrentRewindTime);
                if (snapshot.HasValue)
                {
                    rewindable.RestoreSnapshot(snapshot.Value.snapshot);
                    restoredCount++;
                }
                
                if (recorder.HasReachedOldest())
                {
                    reachedOldestCount++;
                }
            }
            
            bool shouldStop = false;
    
            if (reachedOldestCount >= m_Recorders.Count)
            {
                Debug.Log("[TimeRewind] 所有对象都已到达最旧状态");
                shouldStop = true;
            }
    
            if (CurrentRewindTime <= 0)
            {
                Debug.Log("[TimeRewind] 回溯时间已用完");
                shouldStop = true;
            }
    
            if (m_TargetRewindTime > 0 && Mathf.Approximately(CurrentRewindTime, m_TargetRewindTime))
            {
                shouldStop = true;
            }
    
            if (shouldStop)
            {
                StopRewind();
            }
        }
        
        /// <summary>
        ///   <para>为指定时间应用所有对象的状态</para>
        /// </summary>
        /// <param name="targetTime">目标时间</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyRewindForTime(float targetTime)
        {
            int restoredCount = 0;
    
            foreach (var kvp in m_Recorders)
            {
                var rewindable = kvp.Key;
                var recorder = kvp.Value;
        
                if (rewindable == null || !rewindable.CanRewind) continue;
        
                var snapshot = recorder.GetSnapshotAtTime(targetTime);
                if (snapshot.HasValue)
                {
                    rewindable.RestoreSnapshot(snapshot.Value.snapshot);
                    restoredCount++;
                    Debug.Log($"[TimeRewind] 应用 {rewindable.GetType().Name} 到时间 {snapshot.Value.timestamp:F2}");
                }
            }
    
            Debug.Log($"[TimeRewind] 在时间 {targetTime:F2} 恢复了 {restoredCount} 个对象");
        }
    }
}

#endif