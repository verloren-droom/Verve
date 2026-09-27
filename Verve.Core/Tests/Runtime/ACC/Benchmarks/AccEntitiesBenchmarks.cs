// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve.Tests.ACC.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;
    using AccWorld = Verve.World;
    using EcsWorld = Unity.Entities.World;

    /// <summary>
    ///   <para>ACC 与 Entities 实测；各路径执行相同的数据更新并核验结果。</para>
    /// </summary>
    [Category("ACC.Benchmark")]
    public class AccEntitiesBenchmarks
    {
        /// <summary>
        ///   <para>移动数据；两种框架共用同一结构布局。</para>
        /// </summary>
        public struct Movement : IComponent, IComponentData
        {
            /// <summary>
            ///   <para>位置。</para>
            /// </summary>
            public float X, Y, Z;
            /// <summary>
            ///   <para>速度。</para>
            /// </summary>
            public float Vx, Vy, Vz;
        }

        /// <summary>
        ///   <para>移动能力；每个行动者拥有一个实例。</para>
        /// </summary>
        public sealed class MovementCapability : Capability
        {
            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime)
            {
                ref var value = ref this.GetComponent<Movement>();
                Move(ref value);
            }
        }

        /// <summary>
        ///   <para>Burst 移动任务；通过数据块批量更新。</para>
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        public struct MoveJob : IJobChunk
        {
            /// <summary>
            ///   <para>组件访问句柄。</para>
            /// </summary>
            public ComponentTypeHandle<Movement> Handle;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var values = chunk.GetNativeArray(ref Handle);
                for (int i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    Move(ref value);
                    values[i] = value;
                }
            }
        }

        /// <summary>
        ///   <para>预热轮数。</para>
        /// </summary>
        private const int Warmups = 2;
        /// <summary>
        ///   <para>独立采样轮数。</para>
        /// </summary>
        private const int Samples = 9;
        /// <summary>
        ///   <para>每轮稳态更新次数。</para>
        /// </summary>
        private const int Iterations = 32;

        /// <summary>
        ///   <para>比较创建、查询、更新与销毁；不以机器相关的速度比例断言正确性。</para>
        /// </summary>
        /// <param name="count">对象数量。</param>
        [TestCase(1000), TestCase(10000), TestCase(100000)]
        public void CompareLifecycleAndUpdates(int count)
        {
            TestContext.Out.WriteLine($"Environment: Unity={Application.unityVersion}; Entities=1.4 API; CPU={SystemInfo.processorType}; OS={SystemInfo.operatingSystem}; Burst={BurstCompiler.IsEnabled}; Stopwatch={Stopwatch.Frequency}; warmup={Warmups}; samples={Samples}; iterations={Iterations}");
            using var acc = new AccWorld("Benchmark", new ActorManagerOptions(count, false, 2));
            using var ecs = new EcsWorld("Benchmark", WorldFlags.Game);
            var manager = ecs.EntityManager;
            var archetype = manager.CreateArchetype(typeof(Movement));
            using var query = manager.CreateEntityQuery(ComponentType.ReadWrite<Movement>());
            using var entities = new NativeArray<Entity>(count, Allocator.Persistent);
            var actors = new Actor[count];
            var matches = new List<Actor>(count);
            var initial = new Movement { Vx = 1, Vy = 2, Vz = 3 };

            Action createAcc = () =>
            {
                for (int i = 0; i < count; i++)
                {
                    actors[i] = acc.CreateActor();
                    acc.AddComponent<Movement>(actors[i]) = initial;
                }
            };
            Action createEcs = () =>
            {
                manager.CreateEntity(archetype, entities);
                for (int i = 0; i < count; i++) manager.SetComponentData(entities[i], initial);
            };
            Action destroyAcc = () => { for (int i = 0; i < count; i++) acc.DestroyActor(actors[i]); };
            Action destroyEcs = () => manager.DestroyEntity(entities);

            Measure("ACC.CreateWithData", count, createAcc, teardown: destroyAcc, iterations: 1);
            Measure("Entities.CreateWithData.Batch", count, createEcs, teardown: destroyEcs, iterations: 1);
            Measure("ACC.Destroy", count, destroyAcc, setup: createAcc, iterations: 1);
            Measure("Entities.Destroy.Batch", count, destroyEcs, setup: createEcs, iterations: 1);

            createAcc();
            createEcs();
            Measure("ACC.Query.Collect", count, () => acc.QueryActorsWithComponentNonAlloc<Movement>(matches));
            Measure("Entities.Query.Collect", count, () => { using var found = query.ToEntityArray(Allocator.Temp); AssertCount(found.Length, count); });
            Assert.That(matches.Count, Is.EqualTo(count));

            Measure("ACC.Update.Lookup", count, () =>
            {
                for (int i = 0; i < count; i++) Move(ref acc.GetComponent<Movement>(actors[i]));
            });
            Check(acc.GetComponent<Movement>(actors[0]), 1);
            Measure("Entities.Update.EntityManager", count, () =>
            {
                for (int i = 0; i < count; i++)
                {
                    var value = manager.GetComponentData<Movement>(entities[i]);
                    Move(ref value);
                    manager.SetComponentData(entities[i], value);
                }
            });
            Check(manager.GetComponentData<Movement>(entities[0]), 1);

            for (int i = 0; i < count; i++) acc.AddCapability<MovementCapability>(actors[i]);
            Measure("ACC.Update.Capability", count, () => acc.Capabilities.Update(1f, TickGroup.Gameplay));
            Check(acc.GetComponent<Movement>(actors[count - 1]), 2);
            Measure("Entities.Update.Burst.Run", count, () =>
                new MoveJob { Handle = manager.GetComponentTypeHandle<Movement>(false) }.Run(query));
            Check(manager.GetComponentData<Movement>(entities[count - 1]), 2);
            Measure("Entities.Update.Burst.ScheduleParallelComplete", count, () =>
                new MoveJob { Handle = manager.GetComponentTypeHandle<Movement>(false) }.ScheduleParallel(query, default).Complete());
            Check(manager.GetComponentData<Movement>(entities[count - 1]), 3);
        }

        /// <summary>
        ///   <para>执行一次相同的移动计算。</para>
        /// </summary>
        /// <param name="value">待更新数据。</param>
        private static void Move(ref Movement value)
        {
            value.X += value.Vx;
            value.Y += value.Vy;
            value.Z += value.Vz;
        }

        /// <summary>
        ///   <para>验证查询未丢失对象。</para>
        /// </summary>
        /// <param name="actual">实际数量。</param>
        /// <param name="expected">预期数量。</param>
        private static void AssertCount(int actual, int expected)
        {
            if (actual != expected) throw new InvalidOperationException("Query count mismatch.");
        }

        /// <summary>
        ///   <para>验证更新次数和数据值。</para>
        /// </summary>
        /// <param name="value">更新结果。</param>
        /// <param name="phases">已运行的更新阶段数。</param>
        private static void Check(Movement value, int phases)
        {
            int updates = (Warmups + Samples) * Iterations * phases;
            Assert.That(value.X, Is.EqualTo(updates));
            Assert.That(value.Y, Is.EqualTo(updates * 2));
            Assert.That(value.Z, Is.EqualTo(updates * 3));
        }

        /// <summary>
        ///   <para>采样耗时和当前线程托管分配；准备、清理及日志均在计时外。</para>
        /// </summary>
        /// <param name="name">操作名称。</param>
        /// <param name="count">对象数量。</param>
        /// <param name="action">被测操作。</param>
        /// <param name="setup">每轮准备。</param>
        /// <param name="teardown">每轮清理。</param>
        /// <param name="iterations">每轮调用次数。</param>
        private static void Measure(string name, int count, Action action, Action setup = null, Action teardown = null, int iterations = Iterations)
        {
            var times = new double[Samples];
            var allocations = new double[Samples];
            for (int sample = -Warmups; sample < Samples; sample++)
            {
                setup?.Invoke();
                try
                {
                    long allocated = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    for (int i = 0; i < iterations; i++) action();
                    long elapsed = Stopwatch.GetTimestamp() - start;
                    long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    if (sample < 0) continue;
                    times[sample] = elapsed * 1000d / Stopwatch.Frequency / iterations;
                    allocations[sample] = (double)bytes / iterations;
                }
                finally { teardown?.Invoke(); }
            }
            Array.Sort(times);
            Array.Sort(allocations);
            TestContext.Out.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "ACC_BENCH,{0},{1},{2:F6},{3:F6},{4:F6},{5:F0}", name, count, times[0], times[Samples / 2], times[Samples - 1], allocations[Samples / 2]));
        }
    }
}
