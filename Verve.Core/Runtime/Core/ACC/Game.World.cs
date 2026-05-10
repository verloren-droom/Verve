namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif


    /// <summary>
    ///   <para>游戏入口：世界部分</para>
    /// </summary>
    public static partial class Game
    {
        private static readonly Dictionary<string, World> s_Worlds = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object s_WorldLock = new object();
        private static volatile World s_ActiveWorld;

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
                    if (values.Count == 0) return Array.Empty<World>();
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
            World selected = null;
            bool activeWorldChanged = false;
            lock (s_WorldLock)
            {
                var active = s_ActiveWorld;
                if (active != null && !active.IsDisposed)
                    return active;

                if (active != null && active.IsDisposed)
                {
                    s_ActiveWorld = null;
                    activeWorldChanged = true;
                }

                foreach (var world in s_Worlds.Values)
                {
                    if (world != null && !world.IsDisposed)
                    {
                        s_ActiveWorld = world;
                        selected = world;
                        activeWorldChanged = true;
                        break;
                    }
                }

                if (selected == null)
                {
                    s_ActiveWorld = null;
                }
            }

            if (activeWorldChanged && IsOnMainThread())
            {
                RefreshGameLoopForWorlds();
            }
            return selected;
        }

        /// <summary>
        ///   <para>创建世界</para>
        ///   <para>该入口会同步内部 GameLoop，必须在主线程调用</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        /// <param name="actorManagerOptions">行为者管理选项</param>
        /// <param name="netOptions">网络同步选项</param>
        public static void CreateWorld(string worldName, ActorManagerOptions? actorManagerOptions = null, NetworkSyncOptions? netOptions = null)
        {
            ThrowIfNotOnMainThread("Game.CreateWorld");

            if (string.IsNullOrWhiteSpace(worldName))
                throw new ArgumentException("World name cannot be null or empty");

            bool activateWorld = false;
            lock (s_WorldLock)
            {
                bool createNew = true;
                if (s_Worlds.TryGetValue(worldName, out var existingWorld))
                {
                    if (!existingWorld.IsDisposed)
                    {
                        s_ActiveWorld = existingWorld;
                        activateWorld = true;
                        createNew = false;
                    }
                    else
                    {
                        s_Worlds.Remove(worldName);
                    }
                }

                if (createNew)
                {
                    var world = new World(worldName, actorManagerOptions, netOptions);
                    s_Worlds[worldName] = world;

                    if (s_ActiveWorld == null)
                    {
                        s_ActiveWorld = world;
                        activateWorld = true;
                    }
                }
            }

            if (activateWorld)
            {
                RefreshGameLoopForWorlds();
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
            ThrowIfNotOnMainThread("Game.GotoWorld");

            if (string.IsNullOrWhiteSpace(worldName)) return false;

            bool success = false;
            lock (s_WorldLock)
            {
                if (!s_Worlds.TryGetValue(worldName, out var world) || world.IsDisposed)
                    return false;

                s_ActiveWorld = world;
                success = true;
            }

            if (success)
            {
                RefreshGameLoopForWorlds();
            }
            return true;
        }

        /// <summary>
        ///   <para>销毁世界</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        /// <param name="force">是否强制销毁</param>
        public static void DestroyWorld(string worldName, bool force = false)
        {
            ThrowIfNotOnMainThread("Game.DestroyWorld");

            if (string.IsNullOrWhiteSpace(worldName)) return;

            bool activeChanged = false;
            World worldToDispose = null;
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
                    activeChanged = true;
                }
                else if (s_ActiveWorld == world)
                {
                    s_ActiveWorld = null;
                    activeChanged = true;
                }

                s_Worlds.Remove(worldName);
                worldToDispose = world;
            }

            try
            {
                worldToDispose?.Dispose();
            }
            finally
            {
                if (activeChanged)
                {
                    RefreshGameLoopForWorlds();
                }
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
            ThrowIfNotOnMainThread("Game.DestroyAllWorlds");

            List<World> worldsToDispose = null;
            bool clearedActive = false;
            lock (s_WorldLock)
            {
                worldsToDispose = new List<World>(s_Worlds.Values);
                s_Worlds.Clear();
                clearedActive = s_ActiveWorld != null;
                s_ActiveWorld = null;
            }

            List<Exception> errors = null;
            try
            {
                for (int i = 0; i < worldsToDispose.Count; i++)
                {
                    var world = worldsToDispose[i];
                    if (world == null || world.IsDisposed) continue;

                    try
                    {
                        world.Dispose();
                    }
                    catch (Exception ex)
                    {
                        errors ??= new List<Exception>();
                        errors.Add(ex);
                    }
                }
            }
            finally
            {
                if (clearedActive) RefreshGameLoopForWorlds();
            }

            if (errors == null || errors.Count == 0) return;
            if (errors.Count == 1) throw errors[0];
            throw new AggregateException(errors);
        }

        /// <summary>
        ///   <para>判断当前是否存在正在使用内部 GameLoop 的活跃世界</para>
        /// </summary>
        private static bool HasActiveWorldUsingGameLoop()
        {
            var world = s_ActiveWorld;
            return world != null && !world.IsDisposed;
        }

        /// <summary>
        ///   <para>根据活跃世界状态同步内部 GameLoop</para>
        /// </summary>
        private static void RefreshGameLoopForWorlds()
        {
            if (HasActiveWorldUsingGameLoop())
            {
                TryEnsureGameLoop();
                return;
            }

            if (!HasActiveModuleHandles())
            {
                MarkGameLoopForCleanup();
            }
        }

        /// <summary>
        ///   <para>驱动当前活跃世界执行指定 Tick 分组</para>
        /// </summary>
        private static void TickActiveWorld(float deltaTime, TickGroup tickGroup)
        {
            var world = s_ActiveWorld;
            if (world == null || world.IsDisposed)
            {
                world = GetActiveWorldSlow();
                if (world == null || world.IsDisposed) return;
            }

            try
            {
                world.Tick(deltaTime, tickGroup);
            }
            catch (Exception ex)
            {
                LogError($"World {tickGroup} update error ({world.Name}): {ex}");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        ///   <para>编辑器退出时清理</para>
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnEditorQuitClearWorlds()
        {
            UnityEditor.EditorApplication.quitting -= DestroyAllWorlds;
            UnityEditor.EditorApplication.quitting += DestroyAllWorlds;
        }
#endif
    }
}
