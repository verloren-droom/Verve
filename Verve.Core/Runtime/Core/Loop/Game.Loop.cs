namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
#if UNITY_2018_3_OR_NEWER
    using UnityEngine.LowLevel;
    using UnityEngine.PlayerLoop;
#endif


    /// <summary>
    ///   <para>游戏入口：内部统一游戏循环部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>是否已标记为在下一帧清理内部 GameLoop</para>
        /// </summary>
        private static int s_GameLoopCleanupMarked;
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>主线程的同步上下文</para>
        /// </summary>
        private static SynchronizationContext s_MainThreadContext;

        /// <summary>
        ///   <para>主线程的托管线程编号</para>
        /// </summary>
        private static int s_MainThreadId;

        /// <summary>
        ///   <para>最近一次 GameLoop 初始化失败的异常</para>
        /// </summary>
        private static Exception s_GameLoopInitError;
#endif
#if UNITY_2018_3_OR_NEWER
        /// <summary>
        ///   <para>是否已将内部 GameLoop 安装到 <see cref="PlayerLoop"/></para>
        /// </summary>
        private static bool s_IsGameLoopInstalled;

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 挂接插入位置</para>
        /// </summary>
        private enum PlayerLoopInsertPosition : byte
        {
            Start = 0,
            BeforeAnchor = 1,
            AfterAnchor = 2,
        }

        /// <summary>
        ///   <para>一条 GameLoop 到 <see cref="PlayerLoop"/> 的挂接描述</para>
        /// </summary>
        private readonly struct PlayerLoopBinding
        {
            public readonly Type phaseSystemType;
            public readonly Type anchorSystemType;
            public readonly PlayerLoopInsertPosition insertPosition;
            public readonly PlayerLoopSystem.UpdateFunction updateCallback;

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
        ///   <para>统一维护 GameLoop 对应的 <see cref="PlayerLoop"/> 入口</para>
        /// </summary>
        private static readonly PlayerLoopBinding[] s_PlayerLoopBindings =
        {
            new(typeof(EarlyUpdate), null, PlayerLoopInsertPosition.Start, OnGameLoopEarlyUpdate),
            new(typeof(FixedUpdate), typeof(FixedUpdate.PhysicsFixedUpdate), PlayerLoopInsertPosition.AfterAnchor, OnGameLoopPhysicsUpdate),
            new(typeof(Update), typeof(Update.ScriptRunBehaviourUpdate), PlayerLoopInsertPosition.BeforeAnchor, OnGameLoopGameplayUpdate),
            new(typeof(PreLateUpdate), typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), PlayerLoopInsertPosition.AfterAnchor, OnGameLoopLateUpdate),
        };
#elif UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>基于 <see cref="MonoBehaviour"/> 的内部 GameLoop 驱动</para>
        /// </summary>
        private static GameLoopRunner s_GameLoopRunner;
#endif

#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>运行时加载时缓存主线程上下文</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeMainThreadContext()
        {
            CacheMainThreadContext();
        }

#if UNITY_EDITOR
        /// <summary>
        ///   <para>编辑器域加载时缓存主线程上下文</para>
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeMainThreadContextInEditor()
        {
            CacheMainThreadContext();
        }
#endif

        /// <summary>
        ///   <para>缓存当前主线程上下文</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CacheMainThreadContext()
        {
            s_MainThreadContext = SynchronizationContext.Current;
            s_MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        ///   <para>判断当前是否在主线程</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOnMainThread()
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

        /// <summary>
        ///   <para>在非主线程调用时抛出异常</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfNotOnMainThread(string operationName)
        {
            if (IsOnMainThread()) return;
            throw new InvalidOperationException($"{operationName} must be called on the main thread.");
        }

        /// <summary>
        ///   <para>检查 GameLoop 初始化是否发生在主线程</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryValidateMainThreadAccess(string operationName)
        {
            if (IsOnMainThread()) return true;

            s_GameLoopInitError = new InvalidOperationException($"{operationName} must be called on the main thread.");
            LogError(s_GameLoopInitError.Message);
            return false;
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfNotOnMainThread(string operationName) { }
#endif

        /// <summary>
        ///   <para>保证内部 GameLoop 可用，失败时抛出异常</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureGameLoop()
        {
            if (TryEnsureGameLoop()) return;
            throw CreateGameLoopInitException();
        }

        /// <summary>
        ///   <para>尝试保证内部 GameLoop 可用，失败时仅记录错误</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryEnsureGameLoop()
        {
#if UNITY_5_3_OR_NEWER
            if (!TryValidateMainThreadAccess("GameLoop initialization"))
            {
                return false;
            }

            ClearGameLoopCleanupMark();
            if (IsGameLoopReady()) return true;
            return InitializeGameLoop();
#else
            return true;
#endif
        }

        /// <summary>
        ///   <para>创建初始化失败时抛出的异常对象</para>
        /// </summary>
        private static Exception CreateGameLoopInitException()
        {
#if UNITY_5_3_OR_NEWER
            var initError = s_GameLoopInitError;
            if (initError == null)
            {
                return new InvalidOperationException("GameLoop failed to initialize.");
            }

            return new InvalidOperationException("GameLoop failed to initialize. See inner exception for details.", initError);
#else
            return new InvalidOperationException("GameLoop failed to initialize.");
#endif
        }

        /// <summary>
        ///   <para>尝试执行已经标记好的内部 GameLoop 清理</para>
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
        ///   <para>标记在下一次更新时清理内部 GameLoop</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MarkGameLoopForCleanup()
        {
            Interlocked.Exchange(ref s_GameLoopCleanupMarked, 1);
        }

        /// <summary>
        ///   <para>清除已经设置的内部 GameLoop 清理标记</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearGameLoopCleanupMark()
        {
            Interlocked.Exchange(ref s_GameLoopCleanupMarked, 0);
        }

        /// <summary>
        ///   <para>执行一次 GameLoop 阶段更新</para>
        /// </summary>
        private static void TickGameLoop(float deltaTime, TickGroup group)
        {
            TryCleanupGameLoop();
#if UNITY_5_3_OR_NEWER
            if (!Application.isPlaying) return;
#endif
            TickActiveModules(deltaTime, group);
            TickActiveWorld(deltaTime, group);
        }

        /// <summary>
        ///   <para>判断内部 GameLoop 是否已经处于可用状态</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsGameLoopReady()
        {
#if UNITY_2018_3_OR_NEWER
            return s_IsGameLoopInstalled && HasPlayerLoopBindings(PlayerLoop.GetCurrentPlayerLoop());
#elif UNITY_5_3_OR_NEWER
            return s_GameLoopRunner != null;
#else
            return true;
#endif
        }

#if UNITY_2018_3_OR_NEWER
        /// <summary>
        ///   <para>初始化基于 <see cref="PlayerLoop"/> 的内部 GameLoop</para>
        /// </summary>
        private static bool InitializeGameLoop()
        {
            try
            {
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                if (s_IsGameLoopInstalled && HasPlayerLoopBindings(playerLoop))
                {
                    s_GameLoopInitError = null;
                    return true;
                }

                if (!InstallPlayerLoopBindings(ref playerLoop))
                {
                    throw new InvalidOperationException("Failed to install GameLoop entries into PlayerLoop.");
                }

                PlayerLoop.SetPlayerLoop(playerLoop);
                s_IsGameLoopInstalled = true;
                s_GameLoopInitError = null;
                return true;
            }
            catch (Exception ex)
            {
                s_IsGameLoopInstalled = false;
                s_GameLoopInitError = ex;
                LogError($"Failed to initialize GameLoop: {ex}");
                return false;
            }
        }

        /// <summary>
        ///   <para>清理基于 <see cref="PlayerLoop"/> 的内部 GameLoop</para>
        /// </summary>
        private static void CleanupGameLoop()
        {
            try
            {
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                if (RemovePlayerLoopBindings(ref playerLoop))
                {
                    PlayerLoop.SetPlayerLoop(playerLoop);
                }

                s_IsGameLoopInstalled = false;
                s_GameLoopInitError = null;
            }
            catch (Exception ex)
            {
                LogError($"Failed to cleanup GameLoop: {ex}");
                if (!HasActiveWorldUsingGameLoop() && !HasActiveModuleHandles())
                {
                    MarkGameLoopForCleanup();
                }
            }
        }

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="EarlyUpdate"/> GameLoop 入口</para>
        /// </summary>
        private static void OnGameLoopEarlyUpdate()
        {
            TickGameLoop(Time.deltaTime, TickGroup.Early);
        }

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="FixedUpdate.PhysicsFixedUpdate"/> GameLoop 入口</para>
        /// </summary>
        private static void OnGameLoopPhysicsUpdate()
        {
            TickGameLoop(Time.fixedDeltaTime, TickGroup.Physics);
        }

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="Update.ScriptRunBehaviourUpdate"/> GameLoop 入口</para>
        /// </summary>
        private static void OnGameLoopGameplayUpdate()
        {
            TickGameLoop(Time.deltaTime, TickGroup.Gameplay);
        }

        /// <summary>
        ///   <para><see cref="PlayerLoop"/> 中的 <see cref="PreLateUpdate.ScriptRunBehaviourLateUpdate"/> GameLoop 入口</para>
        /// </summary>
        private static void OnGameLoopLateUpdate()
        {
            TickGameLoop(Time.deltaTime, TickGroup.Late);
        }

        /// <summary>
        ///   <para>把内部 GameLoop 插入到 <see cref="PlayerLoop"/> 对应阶段</para>
        /// </summary>
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
        ///   <para>检查当前 <see cref="PlayerLoop"/> 是否已包含全部 GameLoop 挂接</para>
        /// </summary>
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
        ///   <para>从当前 <see cref="PlayerLoop"/> 中移除全部 GameLoop 挂接</para>
        /// </summary>
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
        ///   <para>创建一个 GameLoop 的 <see cref="PlayerLoop"/> 系统项</para>
        /// </summary>
        private static PlayerLoopSystem CreateGameLoopSystem(PlayerLoopSystem.UpdateFunction updateCallback)
        {
            return new PlayerLoopSystem
            {
                type = typeof(Game),
                updateDelegate = updateCallback
            };
        }

        /// <summary>
        ///   <para>判断系统项是否为指定的 GameLoop 挂接</para>
        /// </summary>
        private static bool IsGameLoopSystem(PlayerLoopSystem system, PlayerLoopSystem.UpdateFunction updateCallback)
        {
            return system.type == typeof(Game) && system.updateDelegate == updateCallback;
        }

        /// <summary>
        ///   <para>检查指定阶段内是否已包含某个 GameLoop 挂接</para>
        /// </summary>
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
        ///   <para>从指定阶段中移除某个 GameLoop 挂接</para>
        /// </summary>
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
        ///   <para>向指定阶段中插入一个 GameLoop 挂接</para>
        /// </summary>
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
        ///   <para>获取一个 GameLoop 挂接在指定阶段中的插入位置</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPlayerLoopInsertIndex(List<PlayerLoopSystem> systems, in PlayerLoopBinding binding)
        {
            switch (binding.insertPosition)
            {
                case PlayerLoopInsertPosition.Start:
                    return 0;
                case PlayerLoopInsertPosition.BeforeAnchor:
                {
                    int anchorIndex = FindPlayerLoopSystemIndex(systems, binding.anchorSystemType);
                    return anchorIndex >= 0 ? anchorIndex : systems.Count;
                }
                case PlayerLoopInsertPosition.AfterAnchor:
                {
                    int anchorIndex = FindPlayerLoopSystemIndex(systems, binding.anchorSystemType);
                    return anchorIndex >= 0 ? anchorIndex + 1 : systems.Count;
                }
                default:
                    return systems.Count;
            }
        }

        /// <summary>
        ///   <para>查找指定系统项在阶段列表中的索引</para>
        /// </summary>
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
#elif UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>初始化基于 <see cref="MonoBehaviour"/> 的内部 GameLoop</para>
        /// </summary>
        private static bool InitializeGameLoop()
        {
            if (s_GameLoopRunner != null)
            {
                s_GameLoopInitError = null;
                return true;
            }

            try
            {
                var runnerObject = new GameObject("[GameLoopRunner]");
                UnityEngine.Object.DontDestroyOnLoad(runnerObject);
                s_GameLoopRunner = runnerObject.AddComponent<GameLoopRunner>();
                s_GameLoopInitError = null;
                return true;
            }
            catch (Exception ex)
            {
                s_GameLoopInitError = ex;
                LogError($"Failed to initialize GameLoop: {ex}");
                return false;
            }
        }

        /// <summary>
        ///   <para>清理基于 <see cref="MonoBehaviour"/> 的内部 GameLoop</para>
        /// </summary>
        private static void CleanupGameLoop()
        {
            try
            {
                if (s_GameLoopRunner != null)
                {
                    UnityEngine.Object.Destroy(s_GameLoopRunner.gameObject);
                    s_GameLoopRunner = null;
                }

                s_GameLoopInitError = null;
            }
            catch (Exception ex)
            {
                LogError($"Failed to cleanup GameLoop: {ex}");
                if (!HasActiveWorldUsingGameLoop() && !HasActiveModuleHandles())
                {
                    MarkGameLoopForCleanup();
                }
            }
        }

        /// <summary>
        ///   <para>旧版本 Unity 使用的 GameLoop 驱动组件</para>
        /// </summary>
        [DefaultExecutionOrder(-1000), DisallowMultipleComponent, AddComponentMenu("Verve/" + nameof(GameLoopRunner))]
        private sealed class GameLoopRunner : ComponentInstanceBase<GameLoopRunner>
        {
            /// <summary>
            ///   <para>驱动 Gameplay 阶段更新</para>
            /// </summary>
            private void Update()
            {
                TickGameLoop(Time.deltaTime, TickGroup.Gameplay);
            }

            /// <summary>
            ///   <para>驱动物理阶段更新</para>
            /// </summary>
            private void FixedUpdate()
            {
                TickGameLoop(Time.fixedDeltaTime, TickGroup.Physics);
            }

            /// <summary>
            ///   <para>驱动 Late 阶段更新</para>
            /// </summary>
            private void LateUpdate()
            {
                TickGameLoop(Time.deltaTime, TickGroup.Late);
            }
        }
#else
        private static void CleanupGameLoop() { }
#endif
    }
}
