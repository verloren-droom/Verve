namespace Verve
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;

    /// <summary>
    ///   <para>模块生命周期执行器。</para>
    /// </summary>
    internal sealed class GameModuleLifecycleRunner
    {
        /// <summary>
        ///   <para>所属模块容器。</para>
        /// </summary>
        private readonly GameModules m_Owner;

        /// <summary>
        ///   <para>底层模块注册表。</para>
        /// </summary>
        private readonly GameModuleRegistry m_Registry;
        /// <summary>
        ///   <para>待报告的观察者错误。</para>
        /// </summary>
        private List<Exception> m_ObserverErrors;

        /// <summary>
        ///   <para>记录操作开始时间。</para>
        /// </summary>
        private long StartObservation() => m_Owner.Observer == null ? 0 : Stopwatch.GetTimestamp();

        /// <summary>
        ///   <para>发送模块操作结果。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="operation">操作。</param>
        /// <param name="started">开始时间戳。</param>
        /// <param name="failure">操作错误。</param>
        private void CompleteObservation(Type moduleType, GameModuleOperation operation, long started, Exception failure)
        {
            var observer = m_Owner.Observer;
            if (observer == null) return;
            var duration = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency);
            try
            {
                using var scope = GameModuleLifecycleScope.Enter();
                observer.OnCompleted(new GameModuleOperationResult(moduleType, operation, duration, failure));
            }
            catch (Exception error)
            {
                // 诊断失败不能打断安装事务或清理；在当前容器操作的出口报告。
                ExceptionUtility.Add(ref m_ObserverErrors, error);
            }
        }

        /// <summary>
        ///   <para>汇总操作和观察者错误。</para>
        /// </summary>
        /// <param name="failure">操作错误。</param>
        private void CompleteOperation(Exception failure)
        {
            var observerFailure = ExceptionUtility.Combine(m_ObserverErrors);
            m_ObserverErrors = null;
            if (observerFailure != null)
                failure = ExceptionUtility.Combine(failure, new InvalidOperationException(
                    "Module observer failed. Completed module operations were not reverted by this diagnostic failure.", observerFailure));
            ExceptionUtility.Rethrow(failure);
        }

        /// <summary>
        ///   <para>执行模块变更。</para>
        /// </summary>
        /// <param name="operation">操作。</param>
        private bool ExecuteChange(Func<CancellationToken, bool> operation)
        {
            using var scope = m_Owner.EnterChangeScope();
            var result = false;
            Exception failure = null;
            try { result = operation(scope.cancellationToken); }
            catch (Exception error) { failure = error; }
            CompleteOperation(failure);
            return result;
        }

        /// <summary>
        ///   <para>异步执行模块变更。</para>
        /// </summary>
        /// <param name="operation">操作。</param>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask<bool> ExecuteChangeAsync(Func<CancellationToken, ValueTask<bool>> operation, CancellationToken ct)
        {
            using var scope = await m_Owner.EnterChangeOperationScopeAsync(ct);
            var result = false;
            Exception failure = null;
            try { result = await operation(scope.cancellationToken); }
            catch (Exception error) { failure = error; }
            CompleteOperation(failure);
            return result;
        }

        /// <summary>
        ///   <para>创建模块生命周期执行器。</para>
        /// </summary>
        /// <param name="owner">所属容器。</param>
        /// <param name="registry">注册表。</param>
        internal GameModuleLifecycleRunner(GameModules owner, GameModuleRegistry registry)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        ///   <para>安装模块。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="moduleType">模块类型。</param>
        internal void Install(Func<GameModule> factory, Type moduleType = null)
        {
            ExecuteChange(ct =>
            {
                InstallImpl(moduleType, factory, ct);
                return true;
            });
        }

        /// <summary>
        ///   <para>异步安装。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="moduleType">模块类型。</param>
        internal async ValueTask InstallAsync(Func<GameModule> factory, CancellationToken ct, Type moduleType = null)
        {
            await ExecuteChangeAsync(async token =>
            {
                token.ThrowIfCancellationRequested();
                await InstallImplAsync(moduleType, factory, token);
                return true;
            }, ct);
        }

        /// <summary>
        ///   <para>创建、接管并安装模块。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="configure">接管后执行的条目配置。</param>
        private GameModule InstallImpl(Type moduleType, Func<GameModule> factory, CancellationToken ct, Action<GameModule> configure = null)
        {
            var started = StartObservation();
            GameModule module = null;
            Exception failure = null;
            try
            {
                module = GameModuleUtility.CreateModule(factory);
                PrepareInstall(module, configure);
                try { InstallPrepared(module, ct); }
                catch { m_Owner.ReleaseModuleOwnership(module); throw; }
            }
            catch (Exception error) { failure = error; }
            CompleteObservation(moduleType ?? module?.GetType(), GameModuleOperation.Install, started, failure);
            ExceptionUtility.Rethrow(failure);
            return module;
        }

        /// <summary>
        ///   <para>异步安装。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="configure">接管后执行的条目配置。</param>
        private async ValueTask<GameModule> InstallImplAsync(Type moduleType, Func<GameModule> factory, CancellationToken ct, Action<GameModule> configure = null)
        {
            var started = StartObservation();
            GameModule module = null;
            Exception failure = null;
            try
            {
                module = GameModuleUtility.CreateModule(factory);
                PrepareInstall(module, configure);
                try { await InstallPreparedAsync(module, ct); }
                catch { m_Owner.ReleaseModuleOwnership(module); throw; }
            }
            catch (Exception error) { failure = error; }
            CompleteObservation(moduleType ?? module?.GetType(), GameModuleOperation.Install, started, failure);
            ExceptionUtility.Rethrow(failure);
            return module;
        }

        /// <summary>
        ///   <para>安装前统一完成存活校验、模块校验、注册表预留和容器所有权登记。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="configure">接管后执行的条目配置。</param>
        private void PrepareInstall(GameModule module, Action<GameModule> configure)
        {
            // 先取得所有权；工厂返回其他容器的实例时不得替调用方释放它。
            m_Owner.TakeModuleOwnership(module);
            try
            {
                m_Owner.ThrowIfNotAlive();
                GameModuleUtility.ThrowIfInvalidModule(module);
                m_Registry.ReserveInstall(module);
                // 配置代码执行前完成接管；配置失败的实例也由容器释放。
                using var scope = GameModuleLifecycleScope.Enter();
                m_Owner.ModuleFactory.Configure(module);
                configure?.Invoke(module);
            }
            catch (Exception failure)
            {
                m_Registry.ReleaseReservedInstall(module);
                try
                {
                    ExceptionUtility.Rethrow(ExceptionUtility.Combine(failure,
                        GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after pre-install failure", m_Owner)));
                    throw;
                }
                finally { m_Owner.ReleaseModuleOwnership(module); }
            }
        }

        /// <summary>
        ///   <para>按模块清单执行同步安装。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        internal void InstallFromManifest(GameModuleManifest manifest)
        {
            ExecuteChange(ct =>
            {
                InstallFromManifestImpl(manifest, ct);
                return true;
            });
        }

        /// <summary>
        ///   <para>按模块清单执行异步安装。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="ct">取消令牌。</param>
        internal async ValueTask InstallFromManifestAsync(GameModuleManifest manifest, CancellationToken ct)
        {
            await ExecuteChangeAsync(async token =>
            {
                await InstallFromManifestImplAsync(manifest, token);
                return true;
            }, ct);
        }

        /// <summary>
        ///   <para>按模块清单执行同步安装流程。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="ct">取消令牌。</param>
        private void InstallFromManifestImpl(GameModuleManifest manifest, CancellationToken ct)
        {
            m_Owner.ThrowIfNotAlive();
            ct.ThrowIfCancellationRequested();

            var descriptors = manifest.GetInstallDescriptors();
            if (descriptors.Count == 0) return;

            var appliedEntries = new List<IGameModule>(descriptors.Count);
            for (int i = 0; i < descriptors.Count; i++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var descriptor = descriptors[i];
                    var module = InstallImpl(descriptor.ModuleType, () => descriptor.CreateModule(m_Owner.ModuleFactory), ct, descriptor.Configure);
                    appliedEntries.Add(module);
                }
                catch (Exception ex)
                {
                    Exception failure = ex;
                    failure = ExceptionUtility.Combine(failure, RollbackAppliedManifestEntries(appliedEntries));
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }
        }

        /// <summary>
        ///   <para>按模块清单执行异步安装流程。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask InstallFromManifestImplAsync(GameModuleManifest manifest, CancellationToken ct)
        {
            m_Owner.ThrowIfNotAlive();
            ct.ThrowIfCancellationRequested();

            var descriptors = manifest.GetInstallDescriptors();
            if (descriptors.Count == 0) return;

            var appliedEntries = new List<IGameModule>(descriptors.Count);
            for (int i = 0; i < descriptors.Count; i++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var descriptor = descriptors[i];
                    var module = await InstallImplAsync(descriptor.ModuleType, () => descriptor.CreateModule(m_Owner.ModuleFactory), ct, descriptor.Configure);
                    appliedEntries.Add(module);
                }
                catch (Exception ex)
                {
                    Exception failure = ex;
                    failure = ExceptionUtility.Combine(failure, await RollbackAppliedManifestEntriesAsync(appliedEntries));
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }
        }

        /// <summary>
        ///   <para>卸载模块。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        internal bool Uninstall(Type moduleType)
        {
            return ExecuteChange(ct =>
                m_Registry.TryGetExactModule(moduleType, out var module) && UninstallInternal(module, ct));
        }

        /// <summary>
        ///   <para>异步卸载。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="ct">取消令牌。</param>
        internal ValueTask<bool> UninstallAsync(Type moduleType, CancellationToken ct)
        {
            return ExecuteChangeAsync(async token => m_Registry.TryGetExactModule(moduleType, out var module) &&
                await UninstallInternalAsync(module, token), ct);
        }

        /// <summary>
        ///   <para>卸载全部。</para>
        /// </summary>
        internal void UninstallAll()
        {
            ExecuteChange(ct =>
            {
                var modules = m_Registry.CopyInstalledModules();
                List<Exception> errors = null;
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    ct.ThrowIfCancellationRequested();
                    try { UninstallInternal(modules[i], ct); }
                    catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
                }
                ExceptionUtility.ThrowIfAny(errors);
                return true;
            });
        }

        /// <summary>
        ///   <para>异步卸载全部。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        internal async ValueTask UninstallAllAsync(CancellationToken ct)
        {
            await ExecuteChangeAsync(async token =>
            {
                var modules = m_Registry.CopyInstalledModules();
                List<Exception> errors = null;
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    token.ThrowIfCancellationRequested();
                    try { await UninstallInternalAsync(modules[i], token); }
                    catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
                }
                ExceptionUtility.ThrowIfAny(errors);
                return true;
            }, ct);
        }

        /// <summary>
        ///   <para>在容器释放期间卸载并释放所有模块。</para>
        /// </summary>
        internal void DisposeAll()
        {
            var modules = m_Registry.CopyInstalledModules();
            m_Registry.ClearTransitionState();
            List<Exception> errors = null;

            try
            {
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    ExceptionUtility.Add(ref errors, TeardownModule(modules[i]));
                }
            }
            finally
            {
                m_Registry.Reset();
            }

            CompleteOperation(ExceptionUtility.Combine(errors));
        }

        /// <summary>
        ///   <para>在容器异步释放期间卸载并释放所有模块。</para>
        /// </summary>
        internal async ValueTask DisposeAllAsync()
        {
            var modules = m_Registry.CopyInstalledModules();
            m_Registry.ClearTransitionState();
            List<Exception> errors = null;

            try
            {
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    ExceptionUtility.Add(ref errors, await TeardownModuleAsync(modules[i]));
                }
            }
            finally
            {
                m_Registry.Reset();
            }

            CompleteOperation(ExceptionUtility.Combine(errors));
        }

        /// <summary>
        ///   <para>同步执行单个模块在容器销毁期间的拆除流程。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private Exception TeardownModule(IGameModule module)
        {
            var started = StartObservation();
            List<Exception> errors = null;
            DetachOwnedTicksDuringDisposeAll(module, ref errors);
            RunUninstallDuringDisposeAll(module, ref errors);
            RemoveFromRegistryDuringDisposeAll(module, ref errors);
            DisposeInstanceDuringDisposeAll(module, ref errors);
            m_Owner.ReleaseModuleOwnership(module);
            var failure = ExceptionUtility.Combine(errors);
            CompleteObservation(module.GetType(), GameModuleOperation.Uninstall, started, failure);
            return failure;
        }

        /// <summary>
        ///   <para>异步执行单个模块在容器销毁期间的拆除流程。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private async ValueTask<Exception> TeardownModuleAsync(IGameModule module)
        {
            var started = StartObservation();
            List<Exception> errors = null;
            DetachOwnedTicksDuringDisposeAll(module, ref errors);
            errors = await RunUninstallDuringDisposeAllAsync(module, errors);
            RemoveFromRegistryDuringDisposeAll(module, ref errors);
            DisposeInstanceDuringDisposeAll(module, ref errors);
            m_Owner.ReleaseModuleOwnership(module);
            var failure = ExceptionUtility.Combine(errors);
            CompleteObservation(module.GetType(), GameModuleOperation.Uninstall, started, failure);
            return failure;
        }

        /// <summary>
        ///   <para>拆离模块拥有的全部 Tick 对象。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="errors">错误。</param>
        private void DetachOwnedTicksDuringDisposeAll(IGameModule module, ref List<Exception> errors)
        {
            try
            {
                m_Owner.DetachOwnedTicks(module);
            }
            catch (Exception ex)
            {
                AddDisposeError(ref errors, module, "detaching owned tick systems", ex);
            }
        }

        /// <summary>
        ///   <para>同步执行模块卸载钩子。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="errors">错误。</param>
        private void RunUninstallDuringDisposeAll(IGameModule module, ref List<Exception> errors)
        {
            GameModuleContext context = null;
            try
            {
                context = m_Owner.CreateContext(module);
                using var lifecycleScope = GameModuleLifecycleScope.Enter();
                RequireGameModule(module).UninstallSync(context, default);
            }
            catch (Exception ex)
            {
                AddDisposeError(ref errors, module, "running uninstall", ex);
            }
            finally
            {
                context?.Invalidate();
            }
        }

        /// <summary>
        ///   <para>异步执行模块卸载钩子。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="errors">错误。</param>
        private async ValueTask<List<Exception>> RunUninstallDuringDisposeAllAsync(
            IGameModule module,
            List<Exception> errors)
        {
            GameModuleContext context = null;
            try
            {
                context = m_Owner.CreateContext(module);
                using var lifecycleScope = GameModuleLifecycleScope.Enter();
                await RequireGameModule(module).UninstallAsync(context, default);
            }
            catch (Exception ex)
            {
                AddDisposeError(ref errors, module, "running uninstall", ex);
            }
            finally
            {
                context?.Invalidate();
            }

            return errors;
        }

        /// <summary>
        ///   <para>从注册表移除已完成销毁的模块。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="errors">错误。</param>
        private void RemoveFromRegistryDuringDisposeAll(IGameModule module, ref List<Exception> errors)
        {
            try
            {
                if (!m_Registry.RemoveInstalledModule(module))
                {
                    AddDisposeError(
                        ref errors,
                        module,
                        "removing from registry",
                        new InvalidOperationException("Module was not removed from the registry during container disposal."));
                }
            }
            catch (Exception ex)
            {
                AddDisposeError(ref errors, module, "removing from registry", ex);
            }
        }

        /// <summary>
        ///   <para>释放模块实例本身。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="errors">错误。</param>
        private void DisposeInstanceDuringDisposeAll(IGameModule module, ref List<Exception> errors)
        {
            try
            {
                DisposeModule(module);
            }
            catch (Exception ex)
            {
                AddDisposeError(ref errors, module, "disposing module", ex);
            }
        }

        /// <summary>
        ///   <para>执行已经完成准备阶段的同步模块安装。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="ct">取消令牌。</param>
        private void InstallPrepared(GameModule module, CancellationToken ct)
        {
            bool added = false;
            bool lifecycleCompleted = false;
            GameModuleContext context = null;
            Exception failure = null;

            try
            {
                context = m_Owner.CreateContext(module, deferTickRegistration: true);
                using var lifecycleScope = GameModuleLifecycleScope.Enter();
                module.InstallSync(context, ct);
                lifecycleCompleted = true;

                if (!m_Owner.IsDisposedOrDisposing)
                {
                    ApplyPreparedInstall(module, context);
                    added = true;
                }
                else
                {
                    failure = new ObjectDisposedException(nameof(GameModules));
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                context?.Invalidate();
            }

            if (!added)
            {
                failure = ExceptionUtility.Combine(failure, ReleaseFailedInstall(module, lifecycleCompleted));
            }

            ExceptionUtility.Rethrow(failure);
        }

        /// <summary>
        ///   <para>执行已经完成准备阶段的异步模块安装。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask InstallPreparedAsync(GameModule module, CancellationToken ct)
        {
            bool added = false;
            bool lifecycleCompleted = false;
            GameModuleContext context = null;
            Exception failure = null;

            try
            {
                context = m_Owner.CreateContext(module, deferTickRegistration: true);
                using var lifecycleScope = GameModuleLifecycleScope.Enter();
                await module.InstallAsync(context, ct);
                lifecycleCompleted = true;

                if (!m_Owner.IsDisposedOrDisposing)
                {
                    using var tickPause = m_Owner.EnterTickPauseScope(ct);
                    ApplyPreparedInstall(module, context);
                    added = true;
                }
                else
                {
                    failure = new ObjectDisposedException(nameof(GameModules));
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                context?.Invalidate();
            }

            if (!added)
            {
                failure = ExceptionUtility.Combine(failure, await ReleaseFailedInstallAsync(module, lifecycleCompleted));
            }

            ExceptionUtility.Rethrow(failure);
        }

        /// <summary>
        ///   <para>应用 Tick 注册并写入注册表；注册表写入失败时撤销已注册 Tick。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="context">上下文。</param>
        private void ApplyPreparedInstall(GameModule module, GameModuleContext context)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (context == null) throw new ArgumentNullException(nameof(context));

            GameModuleContext.AppliedTickRegistrations tickRegistrations = null;
            try
            {
                tickRegistrations = context.ApplyPendingTickSystems();
                m_Registry.AddInstalledModule(module);
                tickRegistrations?.Accept();
                return;
            }
            catch (Exception ex)
            {
                var failure = ExceptionUtility.Combine(
                    ex,
                    tickRegistrations?.Revert());
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        ///   <para>释放安装失败的模块；若安装生命周期已完成，先执行卸载回滚。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="lifecycleCompleted">安装回调是否已完成。</param>
        private Exception ReleaseFailedInstall(GameModule module, bool lifecycleCompleted)
        {
            Exception failure = null;
            try
            {
                if (lifecycleCompleted)
                {
                    failure = RollbackInstalledModuleAfterFailedInstall(module);
                }

                return ExceptionUtility.Combine(failure, GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after a failed install", m_Owner));
            }
            finally
            {
                m_Registry.ReleaseReservedInstall(module);
            }
        }

        /// <summary>
        ///   <para>异步释放安装失败的模块；若安装生命周期已完成，先执行卸载回滚。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="lifecycleCompleted">安装回调是否已完成。</param>
        private async ValueTask<Exception> ReleaseFailedInstallAsync(GameModule module, bool lifecycleCompleted)
        {
            Exception failure = null;
            try
            {
                if (lifecycleCompleted)
                {
                    failure = await RollbackInstalledModuleAfterFailedInstallAsync(module);
                }

                return ExceptionUtility.Combine(failure, GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after a failed install", m_Owner));
            }
            finally
            {
                m_Registry.ReleaseReservedInstall(module);
            }
        }

        /// <summary>
        ///   <para>同步安装生命周期已完成但容器应用失败时，执行卸载回滚。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private Exception RollbackInstalledModuleAfterFailedInstall(GameModule module)
        {
            if (module == null) return null;

            List<Exception> errors = null;
            try
            {
                m_Owner.DetachOwnedTicks(module);
            }
            catch (Exception ex)
            {
                ExceptionUtility.Add(ref errors, ex);
            }

            GameModuleContext context = null;
            try
            {
                context = m_Owner.CreateContext(module);
                using var lifecycleScope = GameModuleLifecycleScope.Enter();
                module.UninstallSync(context, default);
            }
            catch (Exception ex)
            {
                ExceptionUtility.Add(
                    ref errors,
                    new InvalidOperationException(
                        $"Module install rollback failed for {GameModuleUtility.GetTypeDisplayName(module.GetType())}.",
                        ex));
            }
            finally
            {
                context?.Invalidate();
            }

            return ExceptionUtility.Combine(errors);
        }

        /// <summary>
        ///   <para>异步安装生命周期已完成但容器应用失败时，执行卸载回滚。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private async ValueTask<Exception> RollbackInstalledModuleAfterFailedInstallAsync(GameModule module)
        {
            if (module == null) return null;

            List<Exception> errors = null;
            try
            {
                m_Owner.DetachOwnedTicks(module);
            }
            catch (Exception ex)
            {
                ExceptionUtility.Add(ref errors, ex);
            }

            GameModuleContext context = null;
            try
            {
                context = m_Owner.CreateContext(module);
                using var lifecycleScope = GameModuleLifecycleScope.Enter();
                await module.UninstallAsync(context, default);
            }
            catch (Exception ex)
            {
                ExceptionUtility.Add(
                    ref errors,
                    new InvalidOperationException(
                        $"Module install rollback failed for {GameModuleUtility.GetTypeDisplayName(module.GetType())}.",
                        ex));
            }
            finally
            {
                context?.Invalidate();
            }

            return ExceptionUtility.Combine(errors);
        }

        // 卸载是终止操作；失败后继续释放并汇总错误，不恢复已部分拆除的模块或 Tick。
        /// <summary>
        ///   <para>卸载。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="ct">取消令牌。</param>
        private bool UninstallInternal(IGameModule module, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!m_Registry.ReserveUninstall(module)) return false;
            var failure = TeardownModule(module);
            ExceptionUtility.Rethrow(failure);
            return true;
        }

        /// <summary>
        ///   <para>异步卸载。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask<bool> UninstallInternalAsync(IGameModule module, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!m_Registry.ReserveUninstall(module)) return false;
            var failure = await TeardownModuleAsync(module);
            ExceptionUtility.Rethrow(failure);
            return true;
        }

        /// <summary>
        ///   <para>记录容器销毁阶段的模块错误。</para>
        /// </summary>
        /// <param name="errors">错误。</param>
        /// <param name="module">模块。</param>
        /// <param name="operation">操作。</param>
        /// <param name="ex">异常。</param>
        private static void AddDisposeError(ref List<Exception> errors, IGameModule module, string operation, Exception ex)
        {
            if (ex == null) return;

            var moduleName = GameModuleUtility.GetTypeDisplayName(module?.GetType());
            ExceptionUtility.Add(
                ref errors,
                new InvalidOperationException($"Module teardown failed during {operation}: {moduleName}", ex));
        }

        /// <summary>
        ///   <para>回滚已经成功应用的同步清单变更。</para>
        /// </summary>
        /// <param name="appliedEntries">已应用条目。</param>
        private Exception RollbackAppliedManifestEntries(List<IGameModule> appliedEntries)
        {
            if (appliedEntries.Count == 0) return null;

            List<Exception> errors = null;
            for (int i = appliedEntries.Count - 1; i >= 0; i--)
            {
                try
                {
                    UninstallInternal(appliedEntries[i], ct: default);
                }
                catch (Exception ex)
                {
                    ExceptionUtility.Add(ref errors, ex);
                }
            }

            return ExceptionUtility.Combine(errors);
        }

        /// <summary>
        ///   <para>回滚已经成功应用的异步清单变更。</para>
        /// </summary>
        /// <param name="appliedEntries">已应用条目。</param>
        private async ValueTask<Exception> RollbackAppliedManifestEntriesAsync(List<IGameModule> appliedEntries)
        {
            if (appliedEntries.Count == 0) return null;

            List<Exception> errors = null;
            for (int i = appliedEntries.Count - 1; i >= 0; i--)
            {
                try
                {
                    await UninstallInternalAsync(appliedEntries[i], ct: default);
                }
                catch (Exception ex)
                {
                    ExceptionUtility.Add(ref errors, ex);
                }
            }

            return ExceptionUtility.Combine(errors);
        }

        /// <summary>
        ///   <para>获取可安装模块基类；框架不允许直接安装仅实现 <see cref="IGameModule"/> 的对象。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private static GameModule RequireGameModule(IGameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (module is GameModule gameModule) return gameModule;

            var moduleName = GameModuleUtility.GetTypeDisplayName(module.GetType());
            throw new InvalidOperationException(
                $"Installable module {moduleName} must inherit {nameof(GameModule)}.");
        }

        /// <summary>
        ///   <para>直接释放模块实例；失败时立即抛出。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        private void DisposeModule(IGameModule module)
        {
            var disposeFailure = GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module", m_Owner);
            if (disposeFailure != null)
            {
                throw disposeFailure;
            }
        }
    }
}