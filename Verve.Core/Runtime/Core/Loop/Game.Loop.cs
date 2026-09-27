namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    using UnityEngine.LowLevel;
    using UnityEngine.PlayerLoop;
#endif
    
    /// <summary>
    ///   <para>游戏入口；内部统一游戏循环部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>是否已标记为在下一帧清理内部 GameLoop。</para>
        /// </summary>
        private static int s_GameLoopCleanupMarked;
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>主线程的同步上下文。</para>
        /// </summary>
        private static SynchronizationContext s_MainThreadContext;

        /// <summary>
        ///   <para>主线程的托管线程编号。</para>
        /// </summary>
        private static int s_MainThreadId;

#endif
        
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>是否已将内部 GameLoop 安装到 <see cref="PlayerLoop"/>。</para>
        /// </summary>
        private static bool s_IsGameLoopInstalled;

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 挂接插入位置。</para>
        /// </summary>
        private enum PlayerLoopInsertPosition : byte
        {
            /// <summary>
            ///   <para>阶段起始位置。</para>
            /// </summary>
            Start = 0,
            /// <summary>
            ///   <para>锚点之前。</para>
            /// </summary>
            BeforeAnchor = 1,
            /// <summary>
            ///   <para>锚点之后。</para>
            /// </summary>
            AfterAnchor = 2,
        }

        /// <summary>
        ///   <para>循环挂接描述；指定目标阶段、插入位置和回调。</para>
        /// </summary>
        private readonly struct PlayerLoopBinding
        {
            /// <summary>
            ///   <para>阶段系统类型。</para>
            /// </summary>
            public readonly Type phaseSystemType;
            /// <summary>
            ///   <para>锚点系统类型。</para>
            /// </summary>
            public readonly Type anchorSystemType;
            /// <summary>
            ///   <para>插入位置。</para>
            /// </summary>
            public readonly PlayerLoopInsertPosition insertPosition;
            /// <summary>
            ///   <para>更新回调。</para>
            /// </summary>
            public readonly PlayerLoopSystem.UpdateFunction updateCallback;

            /// <summary>
            ///   <para>创建循环挂接描述。</para>
            /// </summary>
            /// <param name="phaseSystemType">阶段系统类型。</param>
            /// <param name="anchorSystemType">锚点系统类型。</param>
            /// <param name="insertPosition">插入位置。</param>
            /// <param name="updateCallback">更新回调。</param>
            public PlayerLoopBinding(
                Type phaseSystemType,
                Type anchorSystemType,
                PlayerLoopInsertPosition insertPosition,
                PlayerLoopSystem.UpdateFunction updateCallback)
            {
                this.phaseSystemType = phaseSystemType ?? throw new ArgumentNullException(nameof(phaseSystemType));
                this.anchorSystemType = anchorSystemType;
                this.insertPosition = insertPosition;
                this.updateCallback = updateCallback ?? throw new ArgumentNullException(nameof(updateCallback));
            }
        }

        /// <summary>
        ///   <para>统一维护 GameLoop 对应的 <see cref="PlayerLoop"/> 入口。</para>
        /// </summary>
        private static readonly PlayerLoopBinding[] s_PlayerLoopBindings =
        {
            new(typeof(EarlyUpdate), null, PlayerLoopInsertPosition.Start, OnGameLoopEarlyUpdate),
            new(typeof(FixedUpdate), typeof(FixedUpdate.PhysicsFixedUpdate), PlayerLoopInsertPosition.AfterAnchor, OnGameLoopPhysicsUpdate),
            new(typeof(Update), typeof(Update.ScriptRunBehaviourUpdate), PlayerLoopInsertPosition.BeforeAnchor, OnGameLoopGameplayUpdate),
            new(typeof(PreLateUpdate), typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), PlayerLoopInsertPosition.AfterAnchor, OnGameLoopLateUpdate),
        };
#endif

#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>运行时加载时缓存主线程上下文。</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeMainThreadContext() => CacheMainThreadContext();

#if UNITY_EDITOR
        /// <summary>
        ///   <para>编辑器域加载时缓存主线程上下文。</para>
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeMainThreadContextInEditor() => CacheMainThreadContext();
#endif

        /// <summary>
        ///   <para>缓存当前主线程上下文。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CacheMainThreadContext()
        {
            s_MainThreadContext = SynchronizationContext.Current;
            s_MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        ///   <para>当前调用是否发生在主线程。</para>
        /// </summary>
        public static bool IsMainThread
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int mainThreadId = Volatile.Read(ref s_MainThreadId);
                if (mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == mainThreadId)
                {
                    return true;
                }

                var mainContext = Volatile.Read(ref s_MainThreadContext);
                var currentContext = SynchronizationContext.Current;
                if (mainContext != null && ReferenceEquals(currentContext, mainContext))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        ///   <para>主线程同步上下文。</para>
        /// </summary>
        public static SynchronizationContext MainThreadContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref s_MainThreadContext);
        }

        /// <summary>
        ///   <para>在非主线程调用时抛出异常。</para>
        /// </summary>
        /// <param name="operationName">操作名称。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotOnMainThread(string operationName)
        {
            if (IsMainThread) return;
            throw new InvalidOperationException($"{operationName} must be called on the main thread.");
        }

#else
        /// <summary>
        ///   <para>是否主线程。</para>
        /// </summary>
        public static bool IsMainThread
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }

        /// <summary>
        ///   <para>主线程上下文。</para>
        /// </summary>
        public static SynchronizationContext MainThreadContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SynchronizationContext.Current;
        }

        /// <summary>
        ///   <para>不在主线程时抛出异常。</para>
        /// </summary>
        /// <param name="operationName">操作名称。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotOnMainThread(string operationName) { }
#endif

        /// <summary>
        ///   <para>确保游戏循环。</para>
        /// </summary>
        private static void EnsureGameLoop()
        {
#if UNITY_5_3_OR_NEWER
            ThrowIfNotOnMainThread("GameLoop initialization");
            ClearGameLoopCleanupMark();
            if (!IsGameLoopReady()) InitializeGameLoop();
#endif
        }

        /// <summary>
        ///   <para>尝试执行已经标记好的内部 GameLoop 清理。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryCleanupGameLoop()
        {
            if (Volatile.Read(ref s_GameLoopCleanupMarked) == 0) return;
            if (Interlocked.Exchange(ref s_GameLoopCleanupMarked, 0) != 0)
            {
                CleanupGameLoop();
            }
        }

        /// <summary>
        ///   <para>标记在下一次更新时清理内部 GameLoop。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MarkGameLoopForCleanup() => Interlocked.Exchange(ref s_GameLoopCleanupMarked, 1);

        /// <summary>
        ///   <para>清除已经设置的内部 GameLoop 清理标记。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearGameLoopCleanupMark() => Interlocked.Exchange(ref s_GameLoopCleanupMarked, 0);

        /// <summary>
        ///   <para>执行一次 GameLoop 阶段更新。</para>
        /// </summary>
        /// <param name="deltaTime">本次更新的时间间隔（秒）。</param>
        /// <param name="group">分组。</param>
        private static void TickGameLoop(float deltaTime, TickGroup group)
        {
            TryCleanupGameLoop();
#if UNITY_5_3_OR_NEWER
            if (!Application.isPlaying) return;
#endif
            TickActiveModules(deltaTime, group);
        }

        /// <summary>
        ///   <para>判断内部 GameLoop 是否已经处于可用状态。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsGameLoopReady()
        {
#if UNITY_5_3_OR_NEWER
            return s_IsGameLoopInstalled && HasPlayerLoopBindings(PlayerLoop.GetCurrentPlayerLoop());
#else
            return true;
#endif
        }

#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>初始化基于 <see cref="PlayerLoop"/> 的内部 GameLoop。</para>
        /// </summary>
        private static void InitializeGameLoop()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (!InstallPlayerLoopBindings(ref playerLoop))
                throw new InvalidOperationException("Required GameLoop phases were not found in PlayerLoop.");
            PlayerLoop.SetPlayerLoop(playerLoop);
            s_IsGameLoopInstalled = true;
        }

        /// <summary>
        ///   <para>清理基于 <see cref="PlayerLoop"/> 的内部 GameLoop。</para>
        /// </summary>
        private static void CleanupGameLoop()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (RemovePlayerLoopBindings(ref playerLoop)) PlayerLoop.SetPlayerLoop(playerLoop);
            s_IsGameLoopInstalled = false;
        }

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="EarlyUpdate"/> GameLoop 入口。</para>
        /// </summary>
        private static void OnGameLoopEarlyUpdate() => TickGameLoop(Time.deltaTime, TickGroup.Early);

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="FixedUpdate.PhysicsFixedUpdate"/> GameLoop 入口。</para>
        /// </summary>
        private static void OnGameLoopPhysicsUpdate() => TickGameLoop(Time.fixedDeltaTime, TickGroup.Physics);

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="Update.ScriptRunBehaviourUpdate"/> GameLoop 入口。</para>
        /// </summary>
        private static void OnGameLoopGameplayUpdate() => TickGameLoop(Time.deltaTime, TickGroup.Gameplay);

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="PreLateUpdate.ScriptRunBehaviourLateUpdate"/> GameLoop 入口。</para>
        /// </summary>
        private static void OnGameLoopLateUpdate() => TickGameLoop(Time.deltaTime, TickGroup.Late);

        /// <summary>
        ///   <para>把内部 GameLoop 插入到 <see cref="PlayerLoop"/> 对应阶段。</para>
        /// </summary>
        /// <param name="loop">循环。</param>
        private static bool InstallPlayerLoopBindings(ref PlayerLoopSystem loop)
        {
            RemovePlayerLoopBindings(ref loop);

            for (int i = 0; i < s_PlayerLoopBindings.Length; i++)
            {
                var binding = s_PlayerLoopBindings[i];
                if (InsertPlayerLoopBinding(ref loop, in binding))
                {
                    continue;
                }

                RemovePlayerLoopBindings(ref loop);
                return false;
            }

            return true;
        }

        /// <summary>
        ///   <para>检查当前 <see cref="PlayerLoop"/> 是否已包含全部 GameLoop 挂接。</para>
        /// </summary>
        /// <param name="loop">循环。</param>
        private static bool HasPlayerLoopBindings(PlayerLoopSystem loop)
        {
            for (int i = 0; i < s_PlayerLoopBindings.Length; i++)
            {
                if (!HasPlayerLoopBinding(loop, in s_PlayerLoopBindings[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>从当前 <see cref="PlayerLoop"/> 中移除全部 GameLoop 挂接。</para>
        /// </summary>
        /// <param name="loop">循环。</param>
        private static bool RemovePlayerLoopBindings(ref PlayerLoopSystem loop)
        {
            bool removed = false;
            for (int i = 0; i < s_PlayerLoopBindings.Length; i++)
            {
                removed |= RemovePlayerLoopBinding(ref loop, in s_PlayerLoopBindings[i]);
            }

            return removed;
        }

        /// <summary>
        ///   <para>创建一个 GameLoop 的 <see cref="PlayerLoop"/> 系统项。</para>
        /// </summary>
        /// <param name="updateCallback">更新回调。</param>
        private static PlayerLoopSystem CreateGameLoopSystem(PlayerLoopSystem.UpdateFunction updateCallback)
        {
            return new PlayerLoopSystem
            {
                type = typeof(Game),
                updateDelegate = updateCallback
            };
        }

        /// <summary>
        ///   <para>判断系统项是否为指定的 GameLoop 挂接。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        /// <param name="updateCallback">更新回调。</param>
        private static bool IsGameLoopSystem(PlayerLoopSystem system, PlayerLoopSystem.UpdateFunction updateCallback) => system.type == typeof(Game) && system.updateDelegate == updateCallback;

        /// <summary>
        ///   <para>检查指定阶段内是否已包含某个 GameLoop 挂接。</para>
        /// </summary>
        /// <param name="loop">循环。</param>
        /// <param name="binding">绑定。</param>
        private static bool HasPlayerLoopBinding(PlayerLoopSystem loop, in PlayerLoopBinding binding)
        {
            var rootSystems = loop.subSystemList;
            if (rootSystems == null || rootSystems.Length == 0) return false;

            for (int i = 0; i < rootSystems.Length; i++)
            {
                if (rootSystems[i].type != binding.phaseSystemType) continue;

                var subSystems = rootSystems[i].subSystemList;
                if (subSystems == null || subSystems.Length == 0) return false;

                for (int j = 0; j < subSystems.Length; j++)
                {
                    if (IsGameLoopSystem(subSystems[j], binding.updateCallback))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///   <para>从指定阶段中移除某个 GameLoop 挂接。</para>
        /// </summary>
        /// <param name="loop">循环。</param>
        /// <param name="binding">绑定。</param>
        private static bool RemovePlayerLoopBinding(ref PlayerLoopSystem loop, in PlayerLoopBinding binding)
        {
            if ((loop.subSystemList?.Length ?? 0) == 0) return false;

            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type != binding.phaseSystemType) continue;

                var phase = loop.subSystemList[i];
                var subSystems = phase.subSystemList;
                if (subSystems == null || subSystems.Length == 0) return false;

                var remaining = new List<PlayerLoopSystem>(subSystems.Length);
                bool removed = false;
                for (int j = 0; j < subSystems.Length; j++)
                {
                    var candidate = subSystems[j];
                    if (IsGameLoopSystem(candidate, binding.updateCallback))
                    {
                        removed = true;
                        continue;
                    }

                    remaining.Add(candidate);
                }

                if (!removed) return false;

                phase.subSystemList = remaining.Count == 0 ? Array.Empty<PlayerLoopSystem>() : remaining.ToArray();
                loop.subSystemList[i] = phase;
                return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>向指定阶段中插入一个 GameLoop 挂接。</para>
        /// </summary>
        /// <param name="loop">循环。</param>
        /// <param name="binding">绑定。</param>
        private static bool InsertPlayerLoopBinding(ref PlayerLoopSystem loop, in PlayerLoopBinding binding)
        {
            if ((loop.subSystemList?.Length ?? 0) == 0) return false;

            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type != binding.phaseSystemType) continue;

                var phase = loop.subSystemList[i];
                var subSystems = new List<PlayerLoopSystem>(phase.subSystemList ?? Array.Empty<PlayerLoopSystem>());
                int insertIndex = GetPlayerLoopInsertIndex(subSystems, in binding);
                subSystems.Insert(insertIndex, CreateGameLoopSystem(binding.updateCallback));
                phase.subSystemList = subSystems.ToArray();
                loop.subSystemList[i] = phase;
                return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>获取一个 GameLoop 挂接在指定阶段中的插入位置。</para>
        /// </summary>
        /// <param name="systems">系统。</param>
        /// <param name="binding">绑定。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPlayerLoopInsertIndex(List<PlayerLoopSystem> systems, in PlayerLoopBinding binding)
        {
            if (binding.insertPosition == PlayerLoopInsertPosition.Start) return 0;
            int anchorIndex = FindPlayerLoopSystemIndex(systems, binding.anchorSystemType);
            if (anchorIndex < 0)
                throw new InvalidOperationException($"PlayerLoop anchor {binding.anchorSystemType} was not found.");
            return binding.insertPosition switch
            {
                PlayerLoopInsertPosition.BeforeAnchor => anchorIndex,
                PlayerLoopInsertPosition.AfterAnchor => anchorIndex + 1,
                _ => throw new ArgumentOutOfRangeException(nameof(binding)),
            };
        }

        /// <summary>
        ///   <para>查找指定系统项在阶段列表中的索引。</para>
        /// </summary>
        /// <param name="systems">系统。</param>
        /// <param name="systemType">系统类型。</param>
        private static int FindPlayerLoopSystemIndex(List<PlayerLoopSystem> systems, Type systemType)
        {
            if (systemType == null) return -1;

            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].type == systemType)
                {
                    return i;
                }
            }

            return -1;
        }
#else
        /// <summary>
        ///   <para>清理游戏循环。</para>
        /// </summary>
        private static void CleanupGameLoop() { }
#endif
    }
}