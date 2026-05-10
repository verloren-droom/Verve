namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif


    /// <summary>
    ///   <para>游戏入口：游戏模块部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>保护模块容器句柄注册表的同步锁</para>
        /// </summary>
        private static readonly object s_ModulesLock = new();

        /// <summary>
        ///   <para>当前活跃的模块容器句柄列表</para>
        /// </summary>
        private static readonly List<GameModulesHandle> s_ModulesHandles = new();

        /// <summary>
        ///   <para>供高频读取使用的模块容器句柄快照</para>
        /// </summary>
        private static GameModulesHandle[] s_ModulesHandleSnapshot = Array.Empty<GameModulesHandle>();

        /// <summary>
        ///   <para>递增生成的模块容器句柄编号</para>
        /// </summary>
        private static int s_NextModulesHandleId;

        /// <summary>
        ///   <para>当模块容器句柄被创建时触发</para>
        /// </summary>
        internal static event Action<GameModulesHandle> OnModulesCreated;

        /// <summary>
        ///   <para>当模块容器句柄被销毁时触发</para>
        /// </summary>
        internal static event Action<GameModulesHandle> OnModulesDestroyed;

        /// <summary>
        ///   <para>创建一个显式持有的模块容器句柄</para>
        /// </summary>
        /// <param name="manifest">模块清单</param>
        /// <param name="scheduler">更新系统调度器</param>
        public static GameModulesHandle CreateModules(GameModuleManifest manifest = null, ITickSystemScheduler scheduler = null)
        {
            ThrowIfNotOnMainThread($"{nameof(Game)}.{nameof(CreateModules)}");

            GameModules target = null;
            bool requestedGameLoop = false;

            try
            {
                target = new GameModules(scheduler);
                if (manifest != null)
                {
                    target.InstallFromManifest(manifest);
                }
                requestedGameLoop = true;
                EnsureGameLoop();
                var handle = RegisterModulesHandle(target);
                GameModuleUtility.InvokeEvent(OnModulesCreated, handle, nameof(OnModulesCreated));
                return handle;
            }
            catch (Exception creationException)
            {
                ExceptionDispatchInfo.Capture(HandleCreateModulesFailure(target, requestedGameLoop, creationException)).Throw();
                throw;
            }
        }

        /// <summary>
        ///   <para>把新建容器注册为活跃句柄</para>
        /// </summary>
        private static GameModulesHandle RegisterModulesHandle(GameModules modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));

            lock (s_ModulesLock)
            {
                var handle = new GameModulesHandle(
                    ++s_NextModulesHandleId,
                    modules);
                s_ModulesHandles.Add(handle);
                RefreshHandlesSnapshotNoLock();
                return handle;
            }
        }

        /// <summary>
        ///   <para>同步处理容器创建失败后的回滚</para>
        /// </summary>
        private static Exception HandleCreateModulesFailure(
            GameModules modules,
            bool requestedGameLoop,
            Exception creationException)
        {
            var failure = creationException;
            if (modules != null)
            {
                try
                {
                    modules.Dispose();
                }
                catch (Exception disposeException)
                {
                    failure = GameModuleUtility.CombineErrors(creationException, disposeException);
                }
            }

#if UNITY_5_3_OR_NEWER
            if (requestedGameLoop && !HasActiveWorldUsingGameLoop() && !HasActiveModuleHandles())
            {
                MarkGameLoopForCleanup();
            }
#endif

            return failure;
        }

        /// <summary>
        ///   <para>复制当前活跃的模块容器句柄快照</para>
        /// </summary>
        /// <param name="output">用于接收句柄快照的列表</param>
        internal static void CopyModuleHandlesTo(List<GameModulesHandle> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            var handleSnapshot = Volatile.Read(ref s_ModulesHandleSnapshot);
            if (handleSnapshot.Length == 0) return;
            output.AddRange(handleSnapshot);
        }

        /// <summary>
        ///   <para>销毁指定的模块容器句柄</para>
        /// </summary>
        internal static void DestroyModules(GameModulesHandle handle)
        {
            if (handle == null) return;
            if (!TryDetachModulesHandle(handle, out var modules))
            {
                return;
            }

            try
            {
                modules.Dispose();
            }
            catch
            {
                if (modules.IsDisposed)
                {
                    FinalizeHandleDestroyed(handle);
                    throw;
                }

                ReattachModulesHandle(handle, modules);
                throw;
            }

            FinalizeHandleDestroyed(handle);
        }

        /// <summary>
        ///   <para>异步销毁指定的模块容器句柄</para>
        /// </summary>
        internal static async ValueTask DestroyModulesAsync(GameModulesHandle handle)
        {
            if (handle == null) return;
            if (!TryDetachModulesHandle(handle, out var modules))
            {
                return;
            }

            try
            {
                await modules.DisposeAsync();
            }
            catch
            {
                if (modules.IsDisposed)
                {
                    FinalizeHandleDestroyed(handle);
                    throw;
                }

                ReattachModulesHandle(handle, modules);
                throw;
            }

            FinalizeHandleDestroyed(handle);
        }

#if UNITY_5_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void ResetModulesOnLoad()
        {
            DestroyAllModuleHandles();
            if (!HasActiveWorldUsingGameLoop() && !HasActiveModuleHandles())
            {
                MarkGameLoopForCleanup();
            }
        }

        /// <summary>
        ///   <para>驱动全部活跃模块容器执行指定 Tick 分组</para>
        /// </summary>
        private static void TickActiveModules(float deltaTime, TickGroup group)
        {
            var handleSnapshot = Volatile.Read(ref s_ModulesHandleSnapshot);
            for (int i = 0; i < handleSnapshot.Length; i++)
            {
                var handle = handleSnapshot[i];
                if (handle == null || !handle.TryGetModules(out var modules)) continue;
                modules.Tick(deltaTime, group);
            }
        }

        /// <summary>
        ///   <para>处理模块容器被直接释放后的静态状态清理</para>
        /// </summary>
        internal static void NotifyModulesDisposed(GameModules modules)
        {
            if (modules == null) return;

            GameModulesHandle handle = null;
            lock (s_ModulesLock)
            {
                for (int i = 0; i < s_ModulesHandles.Count; i++)
                {
                    var candidate = s_ModulesHandles[i];
                    if (candidate == null || !candidate.TryDetachModules(modules))
                    {
                        continue;
                    }

                    handle = candidate;
                    s_ModulesHandles.RemoveAt(i);
                    RefreshHandlesSnapshotNoLock();
                    break;
                }
            }

            if (handle == null) return;
            FinalizeHandleDestroyed(handle);
        }

        /// <summary>
        ///   <para>统一处理模块容器销毁后的事件通知与内部 GameLoop 收尾</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FinalizeHandleDestroyed(GameModulesHandle handle)
        {
            if (handle == null) return;

            try
            {
                GameModuleUtility.InvokeEvent(OnModulesDestroyed, handle, nameof(OnModulesDestroyed));
            }
            finally
            {
                if (!HasActiveWorldUsingGameLoop() && !HasActiveModuleHandles())
                {
                    MarkGameLoopForCleanup();
                }
            }
        }

        /// <summary>
        ///   <para>判断当前是否仍有模块容器使用内部 GameLoop</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasActiveModuleHandles()
        {
            return Volatile.Read(ref s_ModulesHandleSnapshot).Length != 0;
        }

        /// <summary>
        ///   <para>尝试从活跃列表中摘除一个模块容器句柄</para>
        /// </summary>
        private static bool TryDetachModulesHandle(GameModulesHandle handle, out GameModules modules)
        {
            modules = null;
            lock (s_ModulesLock)
            {
                if (!s_ModulesHandles.Remove(handle))
                {
                    return false;
                }

                if (!handle.TryDetachModules(out modules))
                {
                    RefreshHandlesSnapshotNoLock();
                    return false;
                }

                RefreshHandlesSnapshotNoLock();
                return true;
            }
        }

        /// <summary>
        ///   <para>在销毁失败且容器尚未真正释放时，把句柄重新挂回活跃列表</para>
        /// </summary>
        private static void ReattachModulesHandle(GameModulesHandle handle, GameModules modules)
        {
            if (handle == null || modules == null) return;

            lock (s_ModulesLock)
            {
                handle.AttachModules(modules);
                s_ModulesHandles.Add(handle);
                RefreshHandlesSnapshotNoLock();
            }
        }

        /// <summary>
        ///   <para>销毁全部活跃模块容器句柄</para>
        /// </summary>
        private static void DestroyAllModuleHandles()
        {
            var handleSnapshot = Volatile.Read(ref s_ModulesHandleSnapshot);
            if (handleSnapshot.Length == 0) return;

            List<Exception> errors = null;
            for (int i = 0; i < handleSnapshot.Length; i++)
            {
                try
                {
                    DestroyModules(handleSnapshot[i]);
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }

            GameModuleUtility.ThrowIfErrors(errors);
        }

        /// <summary>
        ///   <para>刷新供高频读取使用的句柄快照</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RefreshHandlesSnapshotNoLock()
        {
            s_ModulesHandleSnapshot = s_ModulesHandles.Count == 0
                ? Array.Empty<GameModulesHandle>()
                : s_ModulesHandles.ToArray();
        }
    }
}