namespace VerveUniEx.Sample
{
    using System;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using System.Collections;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    public abstract class StressTestBase : MonoBehaviour
    {
        [Header("基础配置")]
        [SerializeField] protected float testDuration = 60f;
        [SerializeField] protected int targetInstances = 1000;
        
        protected PerformanceMetrics metrics= new PerformanceMetrics();
        protected bool isRunning;
    
        public abstract string TestName { get; }

        protected TestReport currentReport;


        // 资源追踪器
        protected readonly ResourceTracker m_ResourceTracker = new ResourceTracker();
        
        private int initialGCCount;
        private float lastFrameTime;
        private float maxFrameDelta;

        protected virtual void Start()
        {
            initialGCCount = GC.CollectionCount(0);
        }
        
        public abstract IEnumerator RunTest();
    
        public virtual void Cleanup()
        {
            isRunning = false;
            currentReport = new TestReport(TestName);
            StopAllCoroutines();
        }
    
        protected virtual void UpdateMetrics()
        {
            var metrics = new PerformanceMetrics
            {
                // 已有指标
                FPS = 1f / Time.unscaledDeltaTime,
                GPUTime = AdvancedPerformanceMonitor.GetGPUTime(),
                MemoryUsage = GC.GetTotalMemory(false),
                GCCollections = GC.CollectionCount(0) - initialGCCount,
                CPUTime = Time.deltaTime * 1000,
                MaxLatency = maxFrameDelta * 1000 
            };

            // 延迟峰值计算
            float currentDelta = Time.realtimeSinceStartup - lastFrameTime;
            maxFrameDelta = Mathf.Max(maxFrameDelta, currentDelta - Time.deltaTime);
            lastFrameTime = Time.realtimeSinceStartup;
        }

        public virtual TestReport GenerateReport()
        {
            currentReport.Duration = DateTime.Now - currentReport.StartTime;
            return currentReport;
        }
        
        public class ResourceTracker
        {
            private readonly ConditionalWeakTable<object, StackTrace> m_Allocations = 
                new ConditionalWeakTable<object, StackTrace>();

            public void Track(object obj)
            {
                if (!m_Allocations.TryGetValue(obj, out _))
                {
                    m_Allocations.Add(obj, new StackTrace(true));
                }
            }

            public void GenerateLeakReport()
            {
                var typeCount = new Dictionary<Type, int>();
                foreach (var item in m_Allocations)
                {
                    var type = item.Key.GetType();
                    typeCount[type] = typeCount.ContainsKey(type) ? typeCount[type] + 1 : 1;
                }
                UnityEngine.Debug.Log($"对象类型分布:\n{string.Join("\n", typeCount.Select(kv => $"{kv.Key.Name}: {kv.Value}"))}");
            }
        }

        [Button("启动压力测试")]
        protected virtual void OnStartTest()
        {
            if (isRunning)
            {
                UnityEngine.Debug.LogError($"正在压力测试中！！！");
                return;
            }
            Cleanup();
            isRunning = true;
            StartCoroutine(RunTest());
        }
        
        [Button("停止压力测试")]
        protected virtual void OnStopTest()
        {
            Cleanup();
        }
    }


    [Serializable]
    public struct PerformanceMetrics
    {
        public float FPS;
        public long MemoryUsage;
        public int GCCollections;
        public float CPUTime;
        public float GPUTime;
        
        [Tooltip("单位ms")] 
        public float MaxLatency;
    }
    
    
    public class AdvancedPerformanceMonitor : MonoBehaviour
    {
        // GPU时间测量
        private static readonly List<FrameTiming> m_FrameTimings = new List<FrameTiming>(128);
        
        // 内存分析器
        public class MemorySnapshot
        {
            public long TotalAllocated;
            public long TotalReserved;
            public long TotalUnused;
            public Dictionary<Type, int> TypeAllocations = new Dictionary<Type, int>();
        }
    
        public static MemorySnapshot CaptureMemory()
        {
            var snapshot = new MemorySnapshot();
            snapshot.TotalAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            snapshot.TotalReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
            snapshot.TotalUnused = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong();
            return snapshot;
        }
        
        public static float GetGPUTime()
        {
            FrameTimingManager.CaptureFrameTimings();
            uint count = FrameTimingManager.GetLatestTimings(128, m_FrameTimings.ToArray()); // 错误点
            m_FrameTimings.Clear();
            FrameTimingManager.GetLatestTimings(128, m_FrameTimings.ToArray());
            return (float)m_FrameTimings.Average(ft => ft.gpuFrameTime);
        }
    }

    [Serializable]
    public class TestReport
    {
        public string TestName;
        public DateTime StartTime;
        public TimeSpan Duration;
        
        // 核心指标
        public float AvgFPS;
        public float MinFPS;
        public float MaxFPS;
        public long TotalMemoryMB;
        public int GCCount;
        
        // 高级指标
        public float AvgCPUTime;
        public float MaxLatency;
        public int ThreadContention;
        
        public TestReport(string testName)
        {
            TestName = testName;
            StartTime = DateTime.Now;
        }
        
        // 自定义扩展点
        public Dictionary<string, object> CustomMetrics = new Dictionary<string, object>();
    
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== {TestName} 测试报告 ===");
            sb.AppendLine($"持续时间: {Duration.TotalSeconds:F2}s");
            sb.AppendLine($"平均FPS: {AvgFPS:F2}");
            sb.AppendLine($"峰值内存: {TotalMemoryMB}MB");
            sb.AppendLine($"最大延迟: {MaxLatency:F2}ms");
            sb.AppendLine($"线程冲突: {ThreadContention}次");
            return sb.ToString();
        }
    }
}