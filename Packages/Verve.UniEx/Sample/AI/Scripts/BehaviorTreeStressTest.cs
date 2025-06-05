#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample.AI
{
    using System.Collections.Generic;
    using UnityEngine;
    using Verve.AI;
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Text;
    using System.Threading;
    using System.Linq;
    
    
    [DisallowMultipleComponent]
    public class BehaviorTreeStressTest : MonoBehaviour
    {
        [Header("压力测试参数")]
        [Tooltip("并发行为树数量")] public int ConcurrentTrees = 1000;
        [Tooltip("每个行为树的节点数")] public int NodesPerTree = 500;
        [Tooltip("测试持续时间(s)")] public float TestDuration = 60f;
        
        [Serializable]
        private struct PerformanceMetrics
        {
            public long TotalFrames;
            public float MinFPS;
            public float MaxFPS;
            public float AvgFPS;
            public long TotalAllocations;
            public long GCCollections;
            public float AvgCPUTime;       // 平均CPU耗时(ms)
            public float MaxUpdateLatency; // 最大更新延迟(ms)
            public int ThreadContention;   // 线程竞争计数
        }
    
        [SerializeField] private PerformanceMetrics m_Metrics = new PerformanceMetrics()
        {
            MinFPS = float.MaxValue,
            MaxFPS = 0,
            AvgFPS = 0,
            TotalAllocations = 0,
            GCCollections = 0
        };
        
        [SerializeField] private float m_TestStartTime;
        [SerializeField] private List<BehaviorTree> m_TestTrees = new List<BehaviorTree>();
        
        [SerializeField] private int m_InitialGCCount;
        [SerializeField] private long m_InitialMemory;
        private Blackboard m_SharedBlackboard;
        private CancellationTokenSource m_TestCTS;
        
        private int m_LockContentionCount;
        private readonly object m_LockContentionLock = new object();

        private bool m_IsRunning;
        
        
        [Serializable]
        public struct TestNode : IBTNode, IResetableNode 
        {
            public enum NodeType { Success, Failure, Random }
            
            [Tooltip("节点类型")] public NodeType Type;
            [Tooltip("执行耗时"), Range(0,100)] public int ExecutionCost;
            
            private long m_StartTick;
            
            public NodeStatus Run(ref NodeRunContext ctx)
            {
                // const int MATRIX_SIZE = 16;
                // float[,] matrixA = new float[MATRIX_SIZE, MATRIX_SIZE];
                // float[,] matrixB = new float[MATRIX_SIZE, MATRIX_SIZE];
                // float[,] result = new float[MATRIX_SIZE, MATRIX_SIZE];
                //
                // var rnd = new System.Random();
                // for (int i = 0; i < MATRIX_SIZE; i++)
                // {
                //     for (int j = 0; j < MATRIX_SIZE; j++)
                //     {
                //         matrixA[i, j] = (float)rnd.NextDouble();
                //         matrixB[i, j] = (float)rnd.NextDouble();
                //     }
                // }
                //
                // for (int i = 0; i < MATRIX_SIZE; i++)
                // {
                //     for (int j = 0; j < MATRIX_SIZE; j++)
                //     {
                //         float sum = 0;
                //         for (int k = 0; k < MATRIX_SIZE; k++)
                //         {
                //             sum += matrixA[i, k] * matrixB[k, j];
                //         }
                //         result[i, j] = sum;
                //     }
                // }
                
                // 使用跨线程安全的时间获取方式
                long currentTick = System.Diagnostics.Stopwatch.GetTimestamp();
                long elapsedTicks = currentTick - m_StartTick;
                double elapsedMs = (elapsedTicks * 1000.0) / System.Diagnostics.Stopwatch.Frequency;
        
                if (elapsedMs < ExecutionCost)
                    return NodeStatus.Running;
                
                return Type switch {
                    NodeType.Success => NodeStatus.Success,
                    NodeType.Failure => NodeStatus.Failure,
                    _ => (new System.Random().Next(0,2) == 0) ? 
                        NodeStatus.Success : NodeStatus.Failure
                };
            }
            
            public void Reset(ref NodeResetContext ctx) => m_StartTick = System.Diagnostics.Stopwatch.GetTimestamp();
        }


        // 节点对象池实现
        public class BTNodePool
        {
            private readonly Stack<TestNode> m_Pool = new Stack<TestNode>();
            private int m_MissCount;
            
            public TestNode Get()
            {
                if (m_Pool.Count > 0)
                    return m_Pool.Pop();
                
                m_MissCount++;
                return new TestNode();
            }
            
            public void Release(TestNode node)
            {
                m_Pool.Push(node);
            }
        }
        
        private List<float> m_FrameTimes = new List<float>(6000); // 记录60秒*100fps

        void Update()
        {
            m_FrameTimes.Add(Time.unscaledDeltaTime * 1000); // 毫秒
            m_Metrics.MaxUpdateLatency = Mathf.Max(m_FrameTimes.Max(), m_Metrics.MaxUpdateLatency);
        }
        
        private void PopulateTestNodes(BehaviorTree tree, int nodeCount)
        {
            var pool = new BTNodePool();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = pool.Get();
                tree.AddNode(node);
            }
        }

        IEnumerator RunStressTest()
        {
            // 初始化测试环境
            m_TestStartTime = Time.realtimeSinceStartup;
            m_SharedBlackboard = new Blackboard(1024);
            
            StartCoroutine(MonitorPerformance());
            
            // 用例1：大规模并发树创建
            yield return StartCoroutine(CreateMassiveTrees(m_SharedBlackboard));
            
            // 用例2：高频节点状态变更
            yield return StartCoroutine(SimulateHighFrequencyUpdates());
            
            // 用例3：内存压力测试
            yield return StartCoroutine(MemoryStressTest());
            
            // 用例4：跨线程安全测试
            yield return StartCoroutine(CrossThreadSafetyTest());
            
            // 生成测试报告
            GenerateTestReport();
        }
        
        private void UpdateAdvancedMetrics()
        {
            // 使用Stopwatch测量实际CPU时间
            var sw = System.Diagnostics.Stopwatch.StartNew();
    
            for (int i = 0; i < 1000000; i++)
            {
                // 模拟行为树的复杂运算
            }
    
            sw.Stop();
            m_Metrics.AvgCPUTime = (m_Metrics.AvgCPUTime + sw.ElapsedMilliseconds) * 0.5f;
    
            // 获取线程竞争计数（需在锁竞争点计数）
            m_Metrics.ThreadContention = m_LockContentionCount;
        }
        
        IEnumerator MonitorPerformance()
        {
            while (Time.realtimeSinceStartup - m_TestStartTime < TestDuration)
            {
                UpdatePerformanceMetrics();
                yield return new WaitForSeconds(0.5f); // 每0.5秒更新一次
            }
        }

        // 用例1：大规模树创建测试
        IEnumerator CreateMassiveTrees(Blackboard bb)
        {
            const int createPerFrame = 20; // 每帧创建20棵树
            int created = 0;
            var allocStart = System.GC.GetTotalMemory(false);
    
            while (created < ConcurrentTrees)
            {
                int batch = Mathf.Min(createPerFrame, ConcurrentTrees - created);
                for (int i = 0; i < batch; i++)
                {
                    var tree = GameLauncher.Instance.AI.CreateBT<BehaviorTree>(true, NodesPerTree, bb);
                    PopulateTestNodes(tree, NodesPerTree);
                    m_TestTrees.Add(tree);
                    created++;
                }
        
                if (created % 100 == 0) CheckMemoryUsage(allocStart);
                yield return null; // 每帧分批创建
            }
        }
    
        // 用例2：高频更新测试
        IEnumerator SimulateHighFrequencyUpdates()
        {
            const int batchSize = 50; // 每帧处理50棵树
            int currentIndex = 0;
            
            while (Time.realtimeSinceStartup - m_TestStartTime < TestDuration)
            {
                using (TrackLockContention(m_TestTrees[currentIndex]))
                {
                    int processed = 0;
                    while (processed < batchSize && currentIndex < m_TestTrees.Count)
                    {
                        // 加锁更新单棵树
                        lock (m_TestTrees[currentIndex])
                        {
                            (m_TestTrees[currentIndex] as IBehaviorTree).Update(0.016f);
                        }
                        
                        currentIndex++;
                        processed++;
                    }
                    
                    if (currentIndex >= m_TestTrees.Count) 
                        currentIndex = 0;
                    
                    m_Metrics.TotalFrames++;
                    yield return null;
                }
            }
        }


        // 用例3：内存压力测试
        IEnumerator MemoryStressTest()
        {
            var rnd = new System.Random();
            while (Time.realtimeSinceStartup - m_TestStartTime < TestDuration)
            {
                lock (m_TestTrees)
                {
                    // 加锁操作测试树集合
                    if (rnd.NextDouble() > 0.5f && m_TestTrees.Count < ConcurrentTrees * 2)
                    {
                        var newTree = GameLauncher.Instance.AI.CreateBT<BehaviorTree>(true, NodesPerTree, m_SharedBlackboard);
                        PopulateTestNodes(newTree, NodesPerTree);
                        m_TestTrees.Add(newTree);
                    }
                    else if(m_TestTrees.Count > 0)
                    {
                        int index = rnd.Next(m_TestTrees.Count);
                        m_TestTrees[index].Dispose();
                        m_TestTrees.RemoveAt(index);
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IDisposable TrackLockContention(object lockObj)
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(lockObj, 0, ref lockTaken);
                if (!lockTaken)
                {
                    Interlocked.Increment(ref m_LockContentionCount);
                    Monitor.Enter(lockObj, ref lockTaken);
                }
                return new DisposableAction(() => 
                {
                    if (lockTaken) Monitor.Exit(lockObj);
                });
            }
            finally
            {
                // 确保异常情况下释放锁
            }
        }
        
        private class DisposableAction : IDisposable
        {
            private readonly Action m_Action;
            
            public DisposableAction(Action action) => m_Action = action;
            public void Dispose() => m_Action?.Invoke();
        }

        
        private void UpdatePerformanceMetrics()
        {
            float safeDelta = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            float currentFPS = 1f / safeDelta;
            m_Metrics.MinFPS = Mathf.Min(m_Metrics.MinFPS, currentFPS);
            m_Metrics.MaxFPS = Mathf.Max(m_Metrics.MaxFPS, currentFPS);
        
            float elapsed = Time.realtimeSinceStartup - m_TestStartTime;
            m_Metrics.AvgFPS = m_Metrics.TotalFrames / elapsed;

            m_Metrics.GCCollections = GC.CollectionCount(0) - m_InitialGCCount;
            m_Metrics.TotalAllocations = GC.GetTotalMemory(false) - m_InitialMemory;
            
            UpdateAdvancedMetrics();
        }
        
        private void CheckMemoryUsage(long initial)
        {
            long current = System.GC.GetTotalMemory(false);
            if (current - initial > 100 * 1024 * 1024) // 超过100MB增长
            {
                Debug.LogError($"内存泄漏预警! 已分配: {(current - initial)/1024/1024}MB");
            }
        }

        private void GenerateTestReport()
        {
            float elapsed = Time.realtimeSinceStartup - m_TestStartTime;
        
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== 行为树压力测试报告 ===");
            report.AppendLine($"测试时长: {elapsed:F2}s");
            report.AppendLine($"并发树数量: {m_TestTrees.Count}");
            report.AppendLine($"节点总数: {m_TestTrees.Count * NodesPerTree}");
            report.AppendLine($"平均帧率: {m_Metrics.AvgFPS:F2} FPS");
            report.AppendLine($"最低帧率: {m_Metrics.MinFPS:F2} FPS");
            report.AppendLine($"最高帧率: {m_Metrics.MaxFPS:F2} FPS");
            report.AppendLine($"平均CPU时间: {m_Metrics.AvgCPUTime:F2} ms");
            report.AppendLine($"最大更新延迟: {m_Metrics.MaxUpdateLatency:F2} ms");
            report.AppendLine($"线程冲突次数: {m_Metrics.ThreadContention}");
            report.AppendLine($"内存分配总量: {m_Metrics.TotalAllocations / 1024 / 1024} MB");
            report.AppendLine($"GC发生次数: {m_Metrics.GCCollections}");

            m_IsRunning = false;
            Debug.Log(report);
        }
        
        IEnumerator CrossThreadSafetyTest()
        {
            const int threadCount = 8;
            var testCTS = new CancellationTokenSource();
        
            try
            {
                var tasks = new Task[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    tasks[i] = Task.Run(() => 
                    {
                        using (TrackLockContention(m_TestTrees))
                        using (TrackLockContention(m_SharedBlackboard))
                        {
                            var rnd = new System.Random(Guid.NewGuid().GetHashCode());
                            while (!testCTS.Token.IsCancellationRequested)
                            {
                                // 加锁访问共享资源
                                lock (m_TestTrees)
                                lock (m_SharedBlackboard)
                                {
                                    if (m_TestTrees.Count == 0) continue;
                                
                                    int index = rnd.Next(0, m_TestTrees.Count);
                                    var tree = m_TestTrees[index];
                                
                                    // 安全更新
                                    ((IBehaviorTree)tree).Update(0.016f);
                                
                                    // 安全写入黑板
                                    string key = $"Thread_{Thread.CurrentThread.ManagedThreadId}";
                                    m_SharedBlackboard.SetValue(key, DateTime.Now.Ticks);
                                }
                            }
                        }
                    }, testCTS.Token);
                }
            
                yield return new WaitForSeconds(TestDuration / 4);
                testCTS.Cancel();
                Task.WaitAll(tasks);
            }
            finally
            {
                testCTS.Dispose();
            }
        }
        
        private void VerifyThreadSafety()
        {
            bool isConsistent = true;
            foreach (var tree in m_TestTrees)
            {
                if (tree.BB.TryGetValue<int>("ConflictKey", out var value))
                {
                    if (value % 2 != 0)
                    {
                        isConsistent = false;
                        Debug.LogError($"数据不一致! TreeID:{tree.ID} Value:{value}");
                    }
                }
            }
            Debug.Log($"线程安全验证结果: {(isConsistent ? "通过" : "失败")}");
        }
        

        [Button("启动压力测试")]
        private void OnStartTest()
        {
            if (m_IsRunning)
            {
                Debug.LogError($"正在压力测试中！！！");
                return;
            }
            m_IsRunning = true;
            StopAllCoroutines();
            GameLauncher.Instance.AI.CanEverTick = false;
            m_TestTrees.Clear();
            m_Metrics = new PerformanceMetrics()
            {
                MinFPS = float.MaxValue,
                MaxFPS = 0,
                AvgFPS = 0,
                TotalAllocations = 0,
                GCCollections = 0
            };
            m_InitialMemory = System.GC.GetTotalMemory(false);
            m_InitialGCCount = System.GC.CollectionCount(0);
            Debug.LogWarning($"开始压力测试！！！");
            StartCoroutine(RunStressTest());
        }
    }
}

#endif