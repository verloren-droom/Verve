namespace Verve
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;


    /// <summary>
    ///   <para>模块生命周期执行器</para>
    /// </summary>
    internal sealed class GameModuleLifecycleRunner
    {
        /// <summary>
        ///   <para>清单安装事务中的已应用条目</para>
        /// </summary>
        private readonly struct ManifestInstallEntry
        {
            public readonly IGameModule installedModule;
            public readonly IGameModule replacedModule;

            public ManifestInstallEntry(IGameModule installedModule, IGameModule replacedModule)
            {
                this.installedModule = installedModule;
                this.replacedModule = replacedModule;
            }
        }

        /// <summary>
        ///   <para>所属模块容器</para>
        /// </summary>
        private readonly GameModules m_Owner;

        /// <summary>
        ///   <para>底层模块注册表</para>
        /// </summary>
        private readonly GameModuleRegistry m_Registry;

        internal GameModuleLifecycleRunner(GameModules owner, GameModuleRegistry registry)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        ///   <para>使用模块创建工厂同步安装模块</para>
        /// </summary>
        internal void Install(Func<GameModule> factory)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            var module = GameModuleUtility.CreateModule(factory);
            InstallImpl(
                module,
                invokeChanged: true,
                disposeReplacedModule: true);
        }

        /// <summary>
        ///   <para>使用模块创建工厂异步安装模块</para>
        /// </summary>
        internal async ValueTask InstallAsync(Func<GameModule> factory)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            var module = GameModuleUtility.CreateModule(factory);
            await InstallImplAsync(
                module,
                invokeChanged: true,
                disposeReplacedModule: true);
        }

        /// <summary>
        ///   <para>安装模块的同步流程</para>
        /// </summary>
        /// <returns>需要延迟到清单事务提交后释放的被替换模块；没有则为空</returns>
        private IGameModule InstallImpl(
            GameModule module,
            bool invokeChanged,
            bool disposeReplacedModule)
        {
            var existingModule = PrepareInstall(module);

            bool keepOwnership = false;
            try
            {
                IGameModule deferredDisposeModule;
                if (existingModule != null)
                {
                    deferredDisposeModule = ReplaceExisting(
                        module,
                        existingModule,
                        invokeChanged,
                        disposeReplacedModule);
                }
                else
                {
                    InstallPrepared(module, invokeChanged);
                    deferredDisposeModule = null;
                }

                keepOwnership = true;
                return deferredDisposeModule;
            }
            finally
            {
                if (!keepOwnership)
                {
                    m_Owner.ReleaseModuleOwnership(module);
                }
            }
        }

        /// <summary>
        ///   <para>安装模块的异步流程</para>
        /// </summary>
        /// <returns>需要延迟到清单事务提交后释放的被替换模块；没有则为空</returns>
        private async ValueTask<IGameModule> InstallImplAsync(
            GameModule module,
            bool invokeChanged,
            bool disposeReplacedModule)
        {
            var existingModule = PrepareInstall(module);

            bool keepOwnership = false;
            try
            {
                IGameModule deferredDisposeModule;
                if (existingModule != null)
                {
                    deferredDisposeModule = await ReplaceExistingAsync(
                        module,
                        existingModule,
                        invokeChanged,
                        disposeReplacedModule);
                }
                else
                {
                    await InstallPreparedAsync(module, invokeChanged);
                    deferredDisposeModule = null;
                }

                keepOwnership = true;
                return deferredDisposeModule;
            }
            finally
            {
                if (!keepOwnership)
                {
                    m_Owner.ReleaseModuleOwnership(module);
                }
            }
        }

        /// <summary>
        ///   <para>安装前统一完成存活校验、模块校验、注册表预留和容器所有权登记</para>
        /// </summary>
        private IGameModule PrepareInstall(GameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            IGameModule existingModule;
            try
            {
                m_Owner.ThrowIfNotAlive();
                GameModuleUtility.ThrowIfInvalidModule(module);
                existingModule = m_Registry.ReserveInstall(module);
            }
            catch (Exception ex)
            {
                ThrowPreInstallFailure(ex, module);
                throw;
            }

            try
            {
                m_Owner.TakeModuleOwnership(module);
            }
            catch (Exception ex)
            {
                m_Registry.ReleaseReservedInstall(module);
                ThrowPreInstallFailure(ex, module);
                throw;
            }

            return existingModule;
        }

        /// <summary>
        ///   <para>按模块清单执行同步安装</para>
        /// </summary>
        internal void InstallFromManifest(GameModuleManifest manifest)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            InstallFromManifestImpl(manifest);
        }

        /// <summary>
        ///   <para>按模块清单执行异步安装</para>
        /// </summary>
        internal async ValueTask InstallFromManifestAsync(GameModuleManifest manifest)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            await InstallFromManifestImplAsync(manifest);
        }

        /// <summary>
        ///   <para>按模块清单执行同步安装流程</para>
        /// </summary>
        private void InstallFromManifestImpl(GameModuleManifest manifest)
        {
            m_Owner.ThrowIfNotAlive();

            var installItems = manifest.CreateInstallItems();
            if (installItems.Count == 0) return;

            var appliedEntries = new List<ManifestInstallEntry>(installItems.Count);
            for (int i = 0; i < installItems.Count; i++)
            {
                var module = installItems[i].module;

                try
                {
                    var deferredDisposeModule = InstallImpl(
                        module,
                        invokeChanged: false,
                        disposeReplacedModule: false);

                    appliedEntries.Add(new ManifestInstallEntry(module, deferredDisposeModule));
                }
                catch (Exception ex)
                {
                    Exception failure = ex;
                    failure = GameModuleUtility.CombineErrors(failure, DisposeUnprocessedManifestModules(installItems, i + 1));
                    failure = GameModuleUtility.CombineErrors(failure, RollbackAppliedManifestEntries(appliedEntries));
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }

            CompleteManifestInstall(appliedEntries);
        }

        /// <summary>
        ///   <para>按模块清单执行异步安装流程</para>
        /// </summary>
        private async ValueTask InstallFromManifestImplAsync(GameModuleManifest manifest)
        {
            m_Owner.ThrowIfNotAlive();

            var installItems = manifest.CreateInstallItems();
            if (installItems.Count == 0) return;

            var appliedEntries = new List<ManifestInstallEntry>(installItems.Count);
            for (int i = 0; i < installItems.Count; i++)
            {
                var module = installItems[i].module;

                try
                {
                    var deferredDisposeModule = await InstallImplAsync(
                        module,
                        invokeChanged: false,
                        disposeReplacedModule: false);

                    appliedEntries.Add(new ManifestInstallEntry(module, deferredDisposeModule));
                }
                catch (Exception ex)
                {
                    Exception failure = ex;
                    failure = GameModuleUtility.CombineErrors(failure, DisposeUnprocessedManifestModules(installItems, i + 1));
                    failure = GameModuleUtility.CombineErrors(failure, await RollbackAppliedManifestEntriesAsync(appliedEntries));
                    ExceptionDispatchInfo.Capture(failure).Throw();
                }
            }

            CompleteManifestInstall(appliedEntries);
        }

        /// <summary>
        ///   <para>同步卸载指定精确类型的模块</para>
        /// </summary>
        internal bool Uninstall(Type moduleType, bool dispose)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            if (moduleType == null) return false;
            if (m_Owner.IsDisposedOrDisposing) return false;
            if (!m_Registry.TryGetExactModule(moduleType, out var module)) return false;
            return UninstallInternal(module, dispose, allowDependents: false, invokeChanged: true);
        }

        /// <summary>
        ///   <para>异步卸载指定精确类型的模块</para>
        /// </summary>
        internal async ValueTask<bool> UninstallAsync(Type moduleType, bool dispose)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            if (moduleType == null) return false;
            if (m_Owner.IsDisposedOrDisposing) return false;
            if (!m_Registry.TryGetExactModule(moduleType, out var module)) return false;
            return await UninstallInternalAsync(module, dispose, allowDependents: false, invokeChanged: true);
        }

        /// <summary>
        ///   <para>同步卸载所有模块</para>
        /// </summary>
        internal void UninstallAll(bool dispose)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            if (m_Owner.IsDisposedOrDisposing) return;

            var modules = m_Registry.CreateDependencyOrderedModulesCopy();
            List<Exception> errors = null;

            for (int i = modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    UninstallInternal(modules[i], dispose, allowDependents: false, invokeChanged: true);
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }

            GameModuleUtility.ThrowIfErrors(errors);
        }

        /// <summary>
        ///   <para>异步卸载所有模块</para>
        /// </summary>
        internal async ValueTask UninstallAllAsync(bool dispose)
        {
            using var changeScope = m_Owner.EnterChangeScope();
            if (m_Owner.IsDisposedOrDisposing) return;

            var modules = m_Registry.CreateDependencyOrderedModulesCopy();
            List<Exception> errors = null;

            for (int i = modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    await UninstallInternalAsync(modules[i], dispose, allowDependents: false, invokeChanged: true);
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }

            GameModuleUtility.ThrowIfErrors(errors);
        }

        /// <summary>
        ///   <para>在容器释放期间卸载并释放所有模块</para>
        /// </summary>
        internal void DisposeAll()
        {
            var modules = m_Registry.CreateDependencyOrderedModulesCopy();
            m_Registry.ClearTransitionState();
            List<Exception> errors = null;

            try
            {
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    DisposeModuleDuringDisposeAll(modules[i], ref errors);
                }
            }
            finally
            {
                m_Registry.Reset();
            }

            GameModuleUtility.ThrowIfErrors(errors);
        }

        /// <summary>
        ///   <para>在容器异步释放期间卸载并释放所有模块</para>
        /// </summary>
        internal async ValueTask DisposeAllAsync()
        {
            var modules = m_Registry.CreateDependencyOrderedModulesCopy();
            m_Registry.ClearTransitionState();
            List<Exception> errors = null;

            try
            {
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    errors = await DisposeModuleDuringDisposeAllAsync(modules[i], errors);
                }
            }
            finally
            {
                m_Registry.Reset();
            }

            GameModuleUtility.ThrowIfErrors(errors);
        }

        /// <summary>
        ///   <para>同步执行单个模块在容器销毁期间的拆除流程</para>
        /// </summary>
        private void DisposeModuleDuringDisposeAll(IGameModule module, ref List<Exception> errors)
        {
            if (module == null) return;

            DetachOwnedTicksDuringDisposeAll(module, ref errors);
            RunUninstallDuringDisposeAll(module, ref errors);
            RemoveFromRegistryDuringDisposeAll(module, ref errors);
            DisposeInstanceDuringDisposeAll(module, ref errors);
            m_Owner.ReleaseModuleOwnership(module);
        }

        /// <summary>
        ///   <para>异步执行单个模块在容器销毁期间的拆除流程</para>
        /// </summary>
        private async ValueTask<List<Exception>> DisposeModuleDuringDisposeAllAsync(IGameModule module, List<Exception> errors)
        {
            if (module == null) return errors;

            DetachOwnedTicksDuringDisposeAll(module, ref errors);
            errors = await RunUninstallDuringDisposeAllAsync(module, errors);
            RemoveFromRegistryDuringDisposeAll(module, ref errors);
            DisposeInstanceDuringDisposeAll(module, ref errors);
            m_Owner.ReleaseModuleOwnership(module);
            return errors;
        }

        /// <summary>
        ///   <para>拆离模块拥有的全部 Tick 对象</para>
        /// </summary>
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
        ///   <para>同步执行模块卸载钩子</para>
        /// </summary>
        private void RunUninstallDuringDisposeAll(IGameModule module, ref List<Exception> errors)
        {
            GameModuleContext context = null;
            try
            {
                context = m_Owner.CreateContext(module);
                RequireGameModule(module).UninstallSync(context);
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
        ///   <para>异步执行模块卸载钩子</para>
        /// </summary>
        private async ValueTask<List<Exception>> RunUninstallDuringDisposeAllAsync(
            IGameModule module,
            List<Exception> errors)
        {
            GameModuleContext context = null;
            try
            {
                context = m_Owner.CreateContext(module);
                await RequireGameModule(module).UninstallAsync(context);
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
        ///   <para>从注册表移除已完成销毁的模块</para>
        /// </summary>
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
        ///   <para>释放模块实例本身</para>
        /// </summary>
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
        ///   <para>执行已经完成准备阶段的同步模块安装</para>
        /// </summary>
        private void InstallPrepared(GameModule module, bool invokeChanged)
        {
            bool added = false;
            bool cancelledByOwnerDisposal = false;
            GameModuleContext context = null;
            Exception failure = null;

            try
            {
                context = m_Owner.CreateContext(module);
                module.InstallSync(context);

                if (!m_Owner.IsDisposedOrDisposing)
                {
                    m_Registry.AddInstalledModule(module);
                    added = true;
                }
                else
                {
                    cancelledByOwnerDisposal = true;
                    m_Registry.ReleaseReservedInstall(module);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
                m_Registry.ReleaseReservedInstall(module);
            }
            finally
            {
                context?.Invalidate();
            }

            CompletePreparedInstall(module, added, cancelledByOwnerDisposal, failure, invokeChanged);
        }

        /// <summary>
        ///   <para>执行已经完成准备阶段的异步模块安装</para>
        /// </summary>
        private async ValueTask InstallPreparedAsync(GameModule module, bool invokeChanged)
        {
            bool added = false;
            bool cancelledByOwnerDisposal = false;
            GameModuleContext context = null;
            Exception failure = null;

            try
            {
                context = m_Owner.CreateContext(module);
                await module.InstallAsync(context);

                if (!m_Owner.IsDisposedOrDisposing)
                {
                    m_Registry.AddInstalledModule(module);
                    added = true;
                }
                else
                {
                    cancelledByOwnerDisposal = true;
                    m_Registry.ReleaseReservedInstall(module);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
                m_Registry.ReleaseReservedInstall(module);
            }
            finally
            {
                context?.Invalidate();
            }

            CompletePreparedInstall(module, added, cancelledByOwnerDisposal, failure, invokeChanged);
        }

        /// <summary>
        ///   <para>提交已经准备好的安装结果并处理失败兜底</para>
        /// </summary>
        private void CompletePreparedInstall(
            GameModule module,
            bool added,
            bool cancelledByOwnerDisposal,
            Exception failure,
            bool invokeChanged)
        {
            if (!added)
            {
                failure = GameModuleUtility.CombineErrors(failure, DetachOwnedTicksAndCreateFailure(module));
                failure = GameModuleUtility.CombineErrors(
                    failure,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after a failed install"));
            }

            if (cancelledByOwnerDisposal)
            {
                failure = GameModuleUtility.CombineErrors(failure, new ObjectDisposedException(nameof(GameModules)));
            }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            if (invokeChanged)
            {
                m_Owner.NotifyModulesChanged();
            }
        }

        /// <summary>
        ///   <para>替换已存在的同精确类型模块</para>
        /// </summary>
        private IGameModule ReplaceExisting(
            GameModule module,
            IGameModule existing,
            bool invokeChanged,
            bool disposeExisting)
        {
            ReserveReplacementInstall(module, existing);

            try
            {
                if (!UninstallInternal(existing, dispose: false, allowDependents: true, invokeChanged: false))
                {
                    throw new InvalidOperationException($"Failed to uninstall existing module {existing.GetType().FullName} during replacement.");
                }
            }
            catch (Exception ex)
            {
                m_Registry.ReleaseReservedInstall(module);
                throw GameModuleUtility.CombineErrors(
                    ex,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing replacement module after uninstalling the existing module failed"));
            }

            try
            {
                InstallPrepared(module, invokeChanged: false);
            }
            catch (Exception installException)
            {
                Exception rollbackException = null;
                try
                {
                    InstallFresh(RequireGameModule(existing), invokeChanged: false);
                }
                catch (Exception ex)
                {
                    rollbackException = ex;
                }

                if (rollbackException != null)
                {
                    throw new AggregateException(installException, rollbackException);
                }

                throw;
            }

            Exception existingDisposeFailure = null;
            if (disposeExisting)
            {
                existingDisposeFailure = GameModuleUtility.DisposeModuleAndCreateFailure(existing, "disposing the replaced module");
            }

            if (invokeChanged)
            {
                m_Owner.NotifyModulesChanged();
            }

            if (existingDisposeFailure != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GameModules)} cleanup failed after successfully replacing a module. " +
                    "The module state has already been committed and will not be rolled back automatically.",
                    existingDisposeFailure);
            }

            return disposeExisting ? null : existing;
        }

        /// <summary>
        ///   <para>异步替换已存在的同精确类型模块</para>
        /// </summary>
        private async ValueTask<IGameModule> ReplaceExistingAsync(
            GameModule module,
            IGameModule existing,
            bool invokeChanged,
            bool disposeExisting)
        {
            ReserveReplacementInstall(module, existing);

            try
            {
                if (!await UninstallInternalAsync(existing, dispose: false, allowDependents: true, invokeChanged: false))
                {
                    throw new InvalidOperationException($"Failed to uninstall existing module {existing.GetType().FullName} during replacement.");
                }
            }
            catch (Exception ex)
            {
                m_Registry.ReleaseReservedInstall(module);
                throw GameModuleUtility.CombineErrors(
                    ex,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing replacement module after uninstalling the existing module failed"));
            }

            try
            {
                await InstallPreparedAsync(module, invokeChanged: false);
            }
            catch (Exception installException)
            {
                Exception rollbackException = null;
                try
                {
                    await InstallFreshAsync(RequireGameModule(existing), invokeChanged: false);
                }
                catch (Exception ex)
                {
                    rollbackException = ex;
                }

                if (rollbackException != null)
                {
                    throw new AggregateException(installException, rollbackException);
                }

                throw;
            }

            Exception existingDisposeFailure = null;
            if (disposeExisting)
            {
                existingDisposeFailure = GameModuleUtility.DisposeModuleAndCreateFailure(existing, "disposing the replaced module");
            }

            if (invokeChanged)
            {
                m_Owner.NotifyModulesChanged();
            }

            if (existingDisposeFailure != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GameModules)} cleanup failed after successfully replacing a module. " +
                    "The module state has already been committed and will not be rolled back automatically.",
                    existingDisposeFailure);
            }

            return disposeExisting ? null : existing;
        }

        /// <summary>
        ///   <para>预留替换安装状态，失败时释放新模块实例</para>
        /// </summary>
        private void ReserveReplacementInstall(GameModule module, IGameModule existing)
        {
            try
            {
                m_Registry.ReserveReplacementInstall(module, existing);
            }
            catch (Exception ex)
            {
                throw GameModuleUtility.CombineErrors(
                    ex,
                    GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing replacement module after failing to reserve replacement state"));
            }
        }

        /// <summary>
        ///   <para>执行模块卸载的同步内部流程</para>
        /// </summary>
        private bool UninstallInternal(IGameModule module, bool dispose, bool allowDependents, bool invokeChanged)
        {
            if (module == null) return false;
            if (!m_Registry.ReserveUninstall(module, allowDependents)) return false;

            List<GameModules.OwnedTickRegistration> detachedTicks = null;
            GameModuleContext context = null;

            try
            {
                detachedTicks = m_Owner.DetachOwnedTicks(module);
                context = m_Owner.CreateContext(module);
                RequireGameModule(module).UninstallSync(context);
            }
            catch (Exception uninstallException)
            {
                Exception failure = uninstallException;
                if (detachedTicks != null && detachedTicks.Count > 0)
                {
                    try
                    {
                        m_Owner.ReattachOwnedTicks(detachedTicks);
                    }
                    catch (Exception restoreException)
                    {
                        failure = GameModuleUtility.CombineErrors(failure, restoreException);
                    }
                }

                m_Registry.ReleaseReservedUninstall(module);
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
            finally
            {
                context?.Invalidate();
            }

            CompleteUninstall(module, dispose, invokeChanged);
            return true;
        }

        /// <summary>
        ///   <para>执行模块卸载的异步内部流程</para>
        /// </summary>
        private async ValueTask<bool> UninstallInternalAsync(IGameModule module, bool dispose, bool allowDependents, bool invokeChanged)
        {
            if (module == null) return false;
            if (!m_Registry.ReserveUninstall(module, allowDependents)) return false;

            List<GameModules.OwnedTickRegistration> detachedTicks = null;
            GameModuleContext context = null;

            try
            {
                detachedTicks = m_Owner.DetachOwnedTicks(module);
                context = m_Owner.CreateContext(module);
                await RequireGameModule(module).UninstallAsync(context);
            }
            catch (Exception uninstallException)
            {
                Exception failure = uninstallException;
                if (detachedTicks != null && detachedTicks.Count > 0)
                {
                    try
                    {
                        m_Owner.ReattachOwnedTicks(detachedTicks);
                    }
                    catch (Exception restoreException)
                    {
                        failure = GameModuleUtility.CombineErrors(failure, restoreException);
                    }
                }

                m_Registry.ReleaseReservedUninstall(module);
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
            finally
            {
                context?.Invalidate();
            }

            CompleteUninstall(module, dispose, invokeChanged);
            return true;
        }

        /// <summary>
        ///   <para>提交卸载后的统一收尾逻辑</para>
        /// </summary>
        private void CompleteUninstall(IGameModule module, bool dispose, bool invokeChanged)
        {
            bool removed = m_Registry.RemoveInstalledModule(module);
            if (!removed)
            {
                var moduleName = GameModuleUtility.GetTypeDisplayName(module?.GetType());
                throw new InvalidOperationException(
                    $"Module {moduleName} completed uninstall but was not removed from the registry.");
            }

            m_Owner.ReleaseModuleOwnership(module);

            Exception disposeFailure = null;
            if (dispose)
            {
                disposeFailure = GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after uninstall");
            }

            if (invokeChanged)
            {
                m_Owner.NotifyModulesChanged();
            }

            if (disposeFailure != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GameModules)} cleanup failed after successfully uninstalling a module. " +
                    "The module state has already been committed and will not be rolled back automatically.",
                    disposeFailure);
            }
        }

        /// <summary>
        ///   <para>重新走一遍全新的同步安装准备并完成安装</para>
        /// </summary>
        private void InstallFresh(GameModule module, bool invokeChanged)
        {
            PrepareFreshInstall(module);

            bool keepOwnership = false;
            try
            {
                InstallPrepared(module, invokeChanged);
                keepOwnership = true;
            }
            finally
            {
                if (!keepOwnership)
                {
                    m_Owner.ReleaseModuleOwnership(module);
                }
            }
        }

        /// <summary>
        ///   <para>重新走一遍全新的异步安装准备并完成安装</para>
        /// </summary>
        private async ValueTask InstallFreshAsync(GameModule module, bool invokeChanged)
        {
            PrepareFreshInstall(module);

            bool keepOwnership = false;
            try
            {
                await InstallPreparedAsync(module, invokeChanged);
                keepOwnership = true;
            }
            finally
            {
                if (!keepOwnership)
                {
                    m_Owner.ReleaseModuleOwnership(module);
                }
            }
        }

        /// <summary>
        ///   <para>为回滚场景中的原模块重新安装准备注册表预留和所有权</para>
        /// </summary>
        private void PrepareFreshInstall(GameModule module)
        {
            var existingModule = m_Registry.ReserveInstall(module);
            if (existingModule != null)
            {
                throw new InvalidOperationException($"Failed to prepare module install because type {existingModule.GetType().FullName} is already occupied.");
            }

            try
            {
                m_Owner.TakeModuleOwnership(module);
            }
            catch
            {
                m_Registry.ReleaseReservedInstall(module);
                throw;
            }
        }

        /// <summary>
        ///   <para>回滚安装失败时可能残留的 Tick 注册</para>
        /// </summary>
        private Exception DetachOwnedTicksAndCreateFailure(IGameModule module)
        {
            if (module == null) return null;

            List<Exception> errors = null;
            try
            {
                m_Owner.DetachOwnedTicks(module);
            }
            catch (Exception ex)
            {
                GameModuleUtility.AddError(ref errors, ex);
            }

            return GameModuleUtility.ToCombinedError(errors);
        }

        /// <summary>
        ///   <para>处理 <see cref="GameModuleManifest"/> 创建的模块在进入生命周期前失败时的释放兜底</para>
        /// </summary>
        private static void ThrowPreInstallFailure(Exception failure, IGameModule module)
        {
            failure = GameModuleUtility.CombineErrors(
                failure,
                GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module after pre-install failure"));

            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        /// <summary>
        ///   <para>记录容器销毁阶段的模块错误</para>
        /// </summary>
        private static void AddDisposeError(ref List<Exception> errors, IGameModule module, string operation, Exception ex)
        {
            if (ex == null) return;

            var moduleName = GameModuleUtility.GetTypeDisplayName(module?.GetType());
            GameModuleUtility.AddError(
                ref errors,
                new InvalidOperationException($"Module teardown failed during {operation}: {moduleName}", ex));
        }

        /// <summary>
        ///   <para>释放尚未交给安装流程处理的清单模块实例</para>
        /// </summary>
        private static Exception DisposeUnprocessedManifestModules(
            IReadOnlyList<GameModuleManifest.InstallItem> installItems,
            int startIndex)
        {
            if (startIndex >= installItems.Count) return null;

            List<Exception> errors = null;
            for (int i = installItems.Count - 1; i >= startIndex; i--)
            {
                GameModuleUtility.AddError(
                    ref errors,
                    GameModuleUtility.DisposeModuleAndCreateFailure(
                        installItems[i].module,
                        "disposing module after manifest installation failed"));
            }

            return GameModuleUtility.ToCombinedError(errors);
        }

        /// <summary>
        ///   <para>完成清单事务成功后的延迟释放和事件触发</para>
        /// </summary>
        private void CompleteManifestInstall(List<ManifestInstallEntry> appliedEntries)
        {
            Exception disposeFailure = null;
            for (int i = appliedEntries.Count - 1; i >= 0; i--)
            {
                var replacedModule = appliedEntries[i].replacedModule;
                disposeFailure = GameModuleUtility.CombineErrors(
                    disposeFailure,
                    GameModuleUtility.DisposeModuleAndCreateFailure(replacedModule, "disposing replaced module after a successful manifest transaction"));
            }

            if (appliedEntries.Count > 0)
            {
                m_Owner.NotifyModulesChanged();
            }

            if (disposeFailure != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GameModules)} cleanup failed after successfully committing a manifest install. " +
                    "The module state has already been committed and will not be rolled back automatically.",
                    disposeFailure);
            }
        }

        /// <summary>
        ///   <para>回滚已经成功应用的同步清单变更</para>
        /// </summary>
        private Exception RollbackAppliedManifestEntries(List<ManifestInstallEntry> appliedEntries)
        {
            if (appliedEntries.Count == 0) return null;

            List<Exception> errors = null;
            for (int i = appliedEntries.Count - 1; i >= 0; i--)
            {
                try
                {
                    RollbackManifestEntry(appliedEntries[i]);
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }

            return GameModuleUtility.ToCombinedError(errors);
        }

        /// <summary>
        ///   <para>回滚已经成功应用的异步清单变更</para>
        /// </summary>
        private async ValueTask<Exception> RollbackAppliedManifestEntriesAsync(List<ManifestInstallEntry> appliedEntries)
        {
            if (appliedEntries.Count == 0) return null;

            List<Exception> errors = null;
            for (int i = appliedEntries.Count - 1; i >= 0; i--)
            {
                try
                {
                    await RollbackManifestEntryAsync(appliedEntries[i]);
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }

            return GameModuleUtility.ToCombinedError(errors);
        }

        /// <summary>
        ///   <para>回滚单个同步清单安装条目</para>
        /// </summary>
        private void RollbackManifestEntry(ManifestInstallEntry entry)
        {
            if (entry.installedModule == null && entry.replacedModule == null)
            {
                return;
            }

            var installedModule = entry.installedModule;
            if (installedModule != null)
            {
                bool removed = true;
                if (m_Registry.TryGetExactModule(installedModule.GetType(), out var current)
                    && ReferenceEquals(current, installedModule))
                {
                    removed = UninstallInternal(
                        installedModule,
                        dispose: false,
                        allowDependents: entry.replacedModule != null,
                        invokeChanged: false);
                }

                if (!removed)
                {
                    throw new InvalidOperationException(
                        $"Failed to rollback manifest module {installedModule.GetType().FullName} because it could not be removed.");
                }
            }

            if (entry.replacedModule != null)
            {
                if (m_Registry.TryGetExactModule(entry.replacedModule.GetType(), out var restored))
                {
                    if (!ReferenceEquals(restored, entry.replacedModule))
                    {
                        throw new InvalidOperationException(
                            $"Failed to rollback manifest replacement for {entry.replacedModule.GetType().FullName} because the slot is occupied by another module instance.");
                    }
                }
                else
                {
                    InstallFresh(RequireGameModule(entry.replacedModule), invokeChanged: false);
                }
            }

            DisposeModule(installedModule);
        }

        /// <summary>
        ///   <para>回滚单个异步清单安装条目</para>
        /// </summary>
        private async ValueTask RollbackManifestEntryAsync(ManifestInstallEntry entry)
        {
            if (entry.installedModule == null && entry.replacedModule == null)
            {
                return;
            }

            var installedModule = entry.installedModule;
            if (installedModule != null)
            {
                bool removed = true;
                if (m_Registry.TryGetExactModule(installedModule.GetType(), out var current)
                    && ReferenceEquals(current, installedModule))
                {
                    removed = await UninstallInternalAsync(
                        installedModule,
                        dispose: false,
                        allowDependents: entry.replacedModule != null,
                        invokeChanged: false);
                }

                if (!removed)
                {
                    throw new InvalidOperationException(
                        $"Failed to rollback manifest module {installedModule.GetType().FullName} because it could not be removed.");
                }
            }

            if (entry.replacedModule != null)
            {
                if (m_Registry.TryGetExactModule(entry.replacedModule.GetType(), out var restored))
                {
                    if (!ReferenceEquals(restored, entry.replacedModule))
                    {
                        throw new InvalidOperationException(
                            $"Failed to rollback manifest replacement for {entry.replacedModule.GetType().FullName} because the slot is occupied by another module instance.");
                    }
                }
                else
                {
                    await InstallFreshAsync(RequireGameModule(entry.replacedModule), invokeChanged: false);
                }
            }

            DisposeModule(installedModule);
        }

        /// <summary>
        ///   <para>获取可安装模块基类；框架不允许直接安装仅实现 <see cref="IGameModule"/> 的对象</para>
        /// </summary>
        private static GameModule RequireGameModule(IGameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (module is GameModule gameModule) return gameModule;

            var moduleName = GameModuleUtility.GetTypeDisplayName(module.GetType());
            throw new InvalidOperationException(
                $"Installable module {moduleName} must inherit {nameof(GameModule)}.");
        }

        /// <summary>
        ///   <para>直接释放模块实例；失败时立即抛出</para>
        /// </summary>
        private static void DisposeModule(IGameModule module)
        {
            var disposeFailure = GameModuleUtility.DisposeModuleAndCreateFailure(module, "disposing module");
            if (disposeFailure != null)
            {
                throw disposeFailure;
            }
        }
    }
}