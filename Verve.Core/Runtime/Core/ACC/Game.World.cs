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
    ///   <para>游戏入口：世界部分</para>
    /// </summary>
    public static partial class Game
    {
        private static readonly Dictionary<string, World> s_Worlds = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object s_WorldLock = new object();
        private static volatile World s_ActiveWorld;
        private static int s_UpdateSystemCleanupRequested;
#if UNITY_2018_3_OR_NEWER
        private static PlayerLoopSystem s_OriginalPlayerLoop;
        private static bool s_IsPlayerLoopModified;
#endif
        
        /// <summary>
        ///   <para>世界数量</para>
        /// </summary>
        public static int WorldCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { lock (s_WorldLock) { return s_Worlds.Count; } }
        }
        
        /// <summary>
        ///   <para>所有世界集合</para>
        /// </summary>
        public static IReadOnlyCollection<World> Worlds
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (s_WorldLock)
                {
                    var values = s_Worlds.Values;
                    var result = new World[values.Count];
                    values.CopyTo(result, 0);
                    return result;
                }
            }
        }
        
        /// <summary>
        ///   <para>当前活跃世界</para>
        /// </summary>
        public static World World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var active = s_ActiveWorld;
                if (active != null && !active.IsDisposed)
                {
                    return active;
                }

                return GetActiveWorldSlow();
            }
        }

        /// <summary>
        ///   <para>获取已激活世界</para>
        /// </summary>
        private static World GetActiveWorldSlow()
        {
            lock (s_WorldLock)
            {
                var active = s_ActiveWorld;
                if (active != null && !active.IsDisposed)
                    return active;

                if (active != null && active.IsDisposed)
                {
                    s_ActiveWorld = null;
                }

                foreach (var world in s_Worlds.Values)
                {
                    if (world != null && !world.IsDisposed)
                    {
                        s_ActiveWorld = world;
                        return world;
                    }
                }

                s_ActiveWorld = null;
                RequestUpdateSystemCleanup();
                return null;
            }
        }
        
        /// <summary>
        ///   <para>创建世界</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        /// <param name="actorManagerOptions">行为者管理选项</param>
        /// <param name="netOptions">网络同步选项</param>
        public static void CreateWorld(string worldName, ActorManagerOptions? actorManagerOptions = null, NetworkSyncOptions? netOptions = null)
        {
            if (string.IsNullOrWhiteSpace(worldName))
                throw new ArgumentException("World name cannot be null or empty");

            bool shouldInitializeUpdateSystem = false;
            lock (s_WorldLock)
            {
                if (s_Worlds.TryGetValue(worldName, out var existingWorld))
                {
                    if (!existingWorld.IsDisposed)
                    {
                        s_ActiveWorld = existingWorld;
                        Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 0);
                        return;
                    }
                    s_Worlds.Remove(worldName);
                }
                
                var world = new World(worldName, actorManagerOptions, netOptions);
                s_Worlds[worldName] = world;

                if (s_ActiveWorld == null)
                {
                    s_ActiveWorld = world;
                    shouldInitializeUpdateSystem = true;
                }
            }

            if (shouldInitializeUpdateSystem)
            {
                Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 0);
                InitializeUpdateSystem();
            }
        }

        /// <summary>
        ///   <para>是否存在世界</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasWorld(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return false;
            lock (s_WorldLock)
            {
                return s_Worlds.TryGetValue(worldName, out var world) && !world.IsDisposed;
            }
        }
        
        /// <summary>
        ///   <para>获取世界</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static World GetWorld(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return null;
            lock (s_WorldLock)
            {
                return s_Worlds.TryGetValue(worldName, out var world) && !world.IsDisposed ? world : null;
            }
        }

        /// <summary>
        ///   <para>跳转世界</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        public static bool GotoWorld(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return false;

            lock (s_WorldLock)
            {
                if (!s_Worlds.TryGetValue(worldName, out var world) || world.IsDisposed)
                    return false;

                s_ActiveWorld = world;
                Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 0);
                return true;
            }
        }
        
        /// <summary>
        ///   <para>销毁世界</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        /// <param name="force">是否强制销毁</param>
        public static void DestroyWorld(string worldName, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(worldName)) return;

            lock (s_WorldLock)
            {
                if (!s_Worlds.TryGetValue(worldName, out var world) || world.IsDisposed)
                    return;
                
                if (s_ActiveWorld == world && !force)
                {
                    World otherWorld = null;
                    foreach (var w in s_Worlds.Values)
                    {
                        if (w == null || w == world || w.IsDisposed) continue;
                        otherWorld = w;
                        break;
                    }
                    s_ActiveWorld = otherWorld;
                    if (otherWorld == null)
                    {
                        RequestUpdateSystemCleanup();
                    }
                }
                else if (s_ActiveWorld == world)
                {
                    s_ActiveWorld = null;
                    RequestUpdateSystemCleanup();
                }

                s_Worlds.Remove(worldName);
                world.Dispose();
            }
        }
        
        /// <summary>
        ///   <para>销毁所有世界</para>
        /// </summary>
#if UNITY_5_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void DestroyAllWorlds()
        {
            lock (s_WorldLock)
            {
                RequestUpdateSystemCleanup();
                
                foreach (var world in s_Worlds.Values)
                {
                    if (world.IsDisposed) continue;
                    world.Dispose();
                }
                
                s_Worlds.Clear();
                s_ActiveWorld = null;
            }
        }

        #region 更新系统
 
#if UNITY_2018_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RequestUpdateSystemCleanup()
        {
            Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryCleanupUpdateSystemIfRequested()
        {
            if (Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 0) != 0)
            {
                CleanupUpdateSystem();
            }
        }

        /// <summary>
        ///   <para>初始化<see cref="PlayerLoop"/>更新系统</para>
        /// </summary>
        private static void InitializeUpdateSystem()
        {
            if (s_IsPlayerLoopModified) return;

            try
            {
                s_OriginalPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

                var earlyUpdateSystem = new PlayerLoopSystem
                {
                    type = typeof(Game),
                    updateDelegate = OnWorldEarlyUpdate
                };

                var physicsUpdateSystem = new PlayerLoopSystem
                {
                    type = typeof(Game),
                    updateDelegate = OnWorldPhysicsUpdate
                };

                var gameplayUpdateSystem = new PlayerLoopSystem
                {
                    type = typeof(Game),
                    updateDelegate = OnWorldGameplayUpdate
                };

                var lateUpdateSystem = new PlayerLoopSystem
                {
                    type = typeof(Game),
                    updateDelegate = OnWorldLateUpdate
                };

                var newPlayerLoop = InsertWorldSystems(s_OriginalPlayerLoop, 
                    earlyUpdateSystem,
                    physicsUpdateSystem,
                    gameplayUpdateSystem,
                    lateUpdateSystem);

                PlayerLoop.SetPlayerLoop(newPlayerLoop);
                s_IsPlayerLoopModified = true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize PlayerLoop: {ex.Message}");
            }
        }

        /// <summary>
        ///   <para>清理<see cref="PlayerLoop"/>更新系统</para>
        /// </summary>
        private static void CleanupUpdateSystem()
        {
            if (!s_IsPlayerLoopModified) return;

            try
            {
                PlayerLoop.SetPlayerLoop(s_OriginalPlayerLoop);
                s_IsPlayerLoopModified = false;
                s_OriginalPlayerLoop = default;
            }
            catch (Exception ex)
            {
                LogError($"Failed to cleanup PlayerLoop: {ex.Message}");
            }
        }

        /// <summary>
        ///   <para>早期更新处理函数（对应<see cref="TickGroup.Early"/>）</para>
        /// </summary>
        private static void OnWorldEarlyUpdate()
        {
            TryCleanupUpdateSystemIfRequested();
            var world = s_ActiveWorld;
            if (world?.IsDisposed == false && Application.isPlaying)
            {
                try
                {
                    world.Tick(Time.deltaTime, TickGroup.Early);
                }
                catch (Exception ex)
                {
                    LogError($"World early update error: {ex}");
                }
            }
        }

        /// <summary>
        ///   <para>物理更新处理函数（对应<see cref="TickGroup.Physics"/>）</para>
        /// </summary>
        private static void OnWorldPhysicsUpdate()
        {
            TryCleanupUpdateSystemIfRequested();
            var world = s_ActiveWorld;
            if (world?.IsDisposed == false && Application.isPlaying)
            {
                try
                {
                    world.Tick(Time.fixedDeltaTime, TickGroup.Physics);
                }
                catch (Exception ex)
                {
                    LogError($"World physics update error: {ex}");
                }
            }
        }

        /// <summary>
        ///   <para>游戏逻辑更新处理函数（对应<see cref="TickGroup.Gameplay"/>）</para>
        /// </summary>
        private static void OnWorldGameplayUpdate()
        {
            TryCleanupUpdateSystemIfRequested();
            var world = s_ActiveWorld;
            if (world?.IsDisposed == false && Application.isPlaying)
            {
                try
                {
                    world.Tick(Time.deltaTime, TickGroup.Gameplay);
                }
                catch (Exception ex)
                {
                    LogError($"World gameplay update error: {ex}");
                }
            }
        }

        /// <summary>
        ///   <para>后期更新处理函数（对应<see cref="TickGroup.Late"/>）</para>
        /// </summary>
        private static void OnWorldLateUpdate()
        {
            TryCleanupUpdateSystemIfRequested();
            var world = s_ActiveWorld;
            if (world?.IsDisposed == false && Application.isPlaying)
            {
                try
                {
                    world.Tick(Time.deltaTime, TickGroup.Late);
                }
                catch (Exception ex)
                {
                    LogError($"World late update error: {ex}");
                }
            }
        }

        /// <summary>
        ///   <para>将世界更新系统插入到<see cref="PlayerLoop"/>中</para>
        /// </summary>
        private static PlayerLoopSystem InsertWorldSystems(
            PlayerLoopSystem loop,
            PlayerLoopSystem earlyUpdateSystem,
            PlayerLoopSystem physicsUpdateSystem,
            PlayerLoopSystem gameplayUpdateSystem,
            PlayerLoopSystem lateUpdateSystem)
        {
            var newLoop = loop;

            InsertSystemIntoSubSystem(ref newLoop, typeof(EarlyUpdate),
                null, earlyUpdateSystem, insertAtStart: true);

            InsertSystemIntoSubSystem(ref newLoop, typeof(FixedUpdate),
                typeof(FixedUpdate.PhysicsFixedUpdate), physicsUpdateSystem, insertAfter: true);

            InsertSystemIntoSubSystem(ref newLoop, typeof(Update),
                typeof(Update.ScriptRunBehaviourUpdate), gameplayUpdateSystem, insertAfter: false);

            InsertSystemIntoSubSystem(ref newLoop, typeof(PreLateUpdate),
                typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), lateUpdateSystem, insertAfter: true);

            return newLoop;
        }

        /// <summary>
        ///   <para>将系统插入到指定的子系统中</para>
        /// </summary>
        private static bool InsertSystemIntoSubSystem(
            ref PlayerLoopSystem loop,
            Type subSystemType,
            Type referenceSystemType,
            PlayerLoopSystem systemToInsert,
            bool insertAtStart = false,
            bool insertAfter = false)
        {
            if (loop.subSystemList == null || loop.subSystemList.Length == 0) return false;

            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == subSystemType)
                {
                    var subSystem = loop.subSystemList[i];
                    var newSubSystemList = new List<PlayerLoopSystem>(subSystem.subSystemList ?? Array.Empty<PlayerLoopSystem>());

                    int insertIndex = 0;
                    
                    if (insertAtStart)
                    {
                        insertIndex = 0;
                    }
                    else if (referenceSystemType != null)
                    {
                        bool foundReference = false;
                        for (int j = 0; j < newSubSystemList.Count; j++)
                        {
                            if (newSubSystemList[j].type == referenceSystemType)
                            {
                                foundReference = true;
                                insertIndex = j;
                                if (insertAfter) insertIndex = j + 1;
                                break;
                            }
                        }
                        
                        if (!foundReference)
                        {
                            insertIndex = newSubSystemList.Count;
                        }
                    }
                    else
                    {
                        insertIndex = newSubSystemList.Count;
                    }

                    newSubSystemList.Insert(insertIndex, systemToInsert);
                    subSystem.subSystemList = newSubSystemList.ToArray();
                    loop.subSystemList[i] = subSystem;
                    return true;
                }
            }

            return false;
        }
#elif UNITY_5_3_OR_NEWER
        private static WorldRunner s_WorldRunner;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RequestUpdateSystemCleanup()
        {
            Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 1);
        }
        
        private static void InitializeUpdateSystem()
        {
            if (s_WorldRunner != null) return;

            var runnerObj = new GameObject("[WorldRunner]");
            UnityEngine.Object.DontDestroyOnLoad(runnerObj);
            s_WorldRunner = runnerObj.AddComponent<WorldRunner>();
        }

        private static void CleanupUpdateSystem()
        {
            if (s_WorldRunner != null)
            {
                UnityEngine.Object.Destroy(s_WorldRunner.gameObject);
                s_WorldRunner = null;
            }
        }

        [DefaultExecutionOrder(-1000), DisallowMultipleComponent, AddComponentMenu("Verve/" + nameof(WorldRunner))]
        private sealed class WorldRunner : ComponentInstanceBase<WorldRunner>
        {
            private void Update()
            {
                if (Interlocked.Exchange(ref s_UpdateSystemCleanupRequested, 0) != 0)
                {
                    CleanupUpdateSystem();
                    return;
                }

                try
                {
                    var world = s_ActiveWorld;
                    if (world?.IsDisposed == false)
                    {
                        world.Tick(Time.deltaTime);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"World update error: {ex}");
                }
            }
        }
#else
        private static void InitializeUpdateSystem(float deltaTime = 0.02f) 
        {
            if (s_ActiveWorld?.IsDisposed == false)
            {
                s_ActiveWorld?.Tick(deltaTime);
            }
        }

        private static void CleanupUpdateSystem() { }

        private static void RequestUpdateSystemCleanup() { }
#endif

        #endregion
        
#if UNITY_EDITOR
        /// <summary>
        ///   <para>编辑器退出时清理</para>
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnEditorQuit()
        {
            UnityEditor.EditorApplication.quitting -= DestroyAllWorlds;
            UnityEditor.EditorApplication.quitting += DestroyAllWorlds;
        }
#endif
    }
}