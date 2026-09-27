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
    ///   <para>游戏入口；游戏模块部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>保护模块容器句柄注册表的同步锁。</para>
        /// </summary>
        private static readonly object s_ModulesLock = new();

        /// <summary>
        ///   <para>当前活跃的模块容器句柄列表。</para>
        /// </summary>
        private static readonly List<GameModulesHandle> s_ModulesHandles = new();

        /// <summary>
        ///   <para>尚未结束的容器释放任务。</para>
        /// </summary>
        private static readonly List<Task> s_ModuleDisposals = new();

        /// <summary>
        ///   <para>供高频读取使用的模块容器句柄只读缓存。</para>
        /// </summary>
        private static GameModulesHandle[] s_ModulesHandleCache = Array.Empty<GameModulesHandle>();

        /// <summary>
        ///   <para>递增生成的模块容器句柄编号。</para>
        /// </summary>
        private static int s_NextModulesHandleId;

        /// <summary>
        ///   <para>当模块容器句柄被创建时触发。</para>
        /// </summary>
        internal static event Action<GameModulesHandle> OnModulesCreated;

        /// <summary>
        ///   <para>当模块容器句柄被销毁时触发。</para>
        /// </summary>
        internal static event Action<GameModulesHandle> OnModulesDestroyed;

        /// <summary>
        ///   <para>创建一个显式持有的模块容器句柄。</para>
        /// </summary>
        /// <param name="manifest">模块清单。</param>
        /// <param name="options">容器扩展的创建配置；实例及其释放由容器管理。</param>
        public static GameModulesHandle CreateModules(GameModuleManifest manifest = null, GameModulesOptions options = null)
        {
            ThrowIfNotOnMainThread($"{nameof(Game)}.{nameof(CreateModules)}");
            if (s_ModulesShutdown != null)
                throw new InvalidOperationException("Cannot create modules while the module session is shutting down.");

            GameModules target = null;
            bool requestedGameLoop = false;

            try
            {
                target = new GameModules(options);
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
        ///   <para>把新建容器注册为活跃句柄。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        private static GameModulesHandle RegisterModulesHandle(GameModules modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));

            lock (s_ModulesLock)
            {
                var handle = new GameModulesHandle(
                    ++s_NextModulesHandleId,
                    modules);
                s_ModulesHandles.Add(handle);
                RefreshHandlesCacheNoLock();
                return handle;
            }
        }

        /// <summary>
        ///   <para>同步处理容器创建失败后的回滚。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        /// <param name="requestedGameLoop">请求的游戏循环。</param>
        /// <param name="creationException">创建异常。</param>
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
                    failure = ExceptionUtility.Combine(creationException, disposeException);
                }
            }

#if UNITY_5_3_OR_NEWER
            if (requestedGameLoop && !HasActiveModuleHandles())
            {
                MarkGameLoopForCleanup();
            }
#endif

            return failure;
        }

        /// <summary>
        ///   <para>复制当前活跃的模块容器句柄缓存。</para>
        /// </summary>
        /// <param name="output">用于接收句柄缓存内容的列表。</param>
        internal static void CopyModuleHandlesTo(List<GameModulesHandle> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            var handles = Volatile.Read(ref s_ModulesHandleCache);
            if (handles.Length == 0) return;
            output.AddRange(handles);
        }

        /// <summary>
        ///   <para>销毁指定的模块容器句柄。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
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
        ///   <para>异步销毁指定的模块容器句柄。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        internal static async ValueTask DestroyModulesAsync(GameModulesHandle handle) => await DestroyModulesAsync(handle, default);

        /// <summary>
        ///   <para>异步销毁指定的模块容器句柄。</para>
        /// </summary>
        /// <param name="handle">模块容器句柄。</param>
        /// <param name="ct">取消令牌。</param>
        internal static async ValueTask DestroyModulesAsync(GameModulesHandle handle, CancellationToken ct)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (s_ModulesLock) s_ModuleDisposals.Add(completion.Task);
            try { await DestroyModulesCoreAsync(handle, ct); }
            finally
            {
                completion.SetResult(true);
                lock (s_ModulesLock) s_ModuleDisposals.Remove(completion.Task);
            }
        }

        /// <summary>
        ///   <para>异步拆除容器并完成句柄通知。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="ct">取消令牌。</param>
        private static async ValueTask DestroyModulesCoreAsync(GameModulesHandle handle, CancellationToken ct)
        {
            if (handle == null) return;
            if (!TryDetachModulesHandle(handle, out var modules))
            {
                return;
            }

            try
            {
                await modules.DisposeAsync(ct);
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
        ///   <para>加载时重置模块容器。</para>
        /// </summary>
#if UNITY_5_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void ResetModulesOnLoad()
        {
            if (s_ModulesShutdown != null && !s_ModulesShutdown.IsCompleted)
                throw new InvalidOperationException("The previous module session is still shutting down.");
            DestroyAllModuleHandles();
            s_ModulesShutdown = null;
            if (!HasActiveModuleHandles())
            {
                MarkGameLoopForCleanup();
            }
        }

        /// <summary>
        ///   <para>驱动全部活跃模块容器执行指定 Tick 分组。</para>
        /// </summary>
        /// <param name="deltaTime">本次更新的时间间隔（秒）。</param>
        /// <param name="group">分组。</param>
        private static void TickActiveModules(float deltaTime, TickGroup group)
        {
            var handles = Volatile.Read(ref s_ModulesHandleCache);
            for (int i = 0; i < handles.Length; i++)
            {
                var handle = handles[i];
                if (handle == null || !handle.TryGetModules(out var modules)) continue;
                modules.Tick(deltaTime, group);
            }
        }

        /// <summary>
        ///   <para>处理模块容器被直接释放后的静态状态清理。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
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
                    RefreshHandlesCacheNoLock();
                    break;
                }
            }

            if (handle == null) return;
            FinalizeHandleDestroyed(handle);
        }

        /// <summary>
        ///   <para>统一处理模块容器销毁后的事件通知与内部 GameLoop 收尾。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
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
                if (!HasActiveModuleHandles())
                {
                    MarkGameLoopForCleanup();
                }
            }
        }

        /// <summary>
        ///   <para>判断当前是否仍有模块容器使用内部 GameLoop。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasActiveModuleHandles() => Volatile.Read(ref s_ModulesHandleCache).Length != 0;

        /// <summary>
        ///   <para>尝试从活跃列表中摘除一个模块容器句柄。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="modules">模块。</param>
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
                    RefreshHandlesCacheNoLock();
                    return false;
                }

                RefreshHandlesCacheNoLock();
                return true;
            }
        }

        /// <summary>
        ///   <para>在销毁失败且容器尚未真正释放时，把句柄重新挂回活跃列表。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="modules">模块。</param>
        private static void ReattachModulesHandle(GameModulesHandle handle, GameModules modules)
        {
            if (handle == null || modules == null) return;

            lock (s_ModulesLock)
            {
                handle.AttachModules(modules);
                s_ModulesHandles.Add(handle);
                RefreshHandlesCacheNoLock();
            }
        }

        /// <summary>
        ///   <para>销毁全部活跃模块容器句柄。</para>
        /// </summary>
        private static void DestroyAllModuleHandles()
        {
            var handles = Volatile.Read(ref s_ModulesHandleCache);
            if (handles.Length == 0) return;

            List<Exception> errors = null;
            for (int i = 0; i < handles.Length; i++)
            {
                try
                {
                    DestroyModules(handles[i]);
                }
                catch (Exception ex)
                {
                    ExceptionUtility.Add(ref errors, ex);
                }
            }

            ExceptionUtility.ThrowIfAny(errors);
        }

        /// <summary>
        ///   <para>刷新供高频读取使用的句柄只读缓存。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RefreshHandlesCacheNoLock()
        {
            s_ModulesHandleCache = s_ModulesHandles.Count == 0
                ? Array.Empty<GameModulesHandle>()
                : s_ModulesHandles.ToArray();
        }
    }
}