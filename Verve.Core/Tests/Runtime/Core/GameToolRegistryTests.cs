namespace Verve.Tests.Core
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class GameToolRegistryTests
    {
        private static int s_Created;
        private object m_SavedTools;
        private object m_SavedShutdown;

        [SetUp]
        public void SetUp()
        {
            s_Created = 0;
            m_SavedTools = Field("s_Tools").GetValue(null);
            m_SavedShutdown = Field("s_ModulesShutdown").GetValue(null);
            Field("s_Tools").SetValue(null, null);
            Field("s_ModulesShutdown").SetValue(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            Field("s_Tools").SetValue(null, m_SavedTools);
            Field("s_ModulesShutdown").SetValue(null, m_SavedShutdown);
        }

        private static FieldInfo Field(string name) => Game.ReflectionUtility.FindField(typeof(Game), name,
            BindingFlags.Static | BindingFlags.NonPublic);

        [Test]
        public void EmptyConfiguration_UsesStatelessDefaults()
        {
            Game.ConfigureTools(new GameToolConfiguration());
            Assert.That(Game.Serializer, Is.TypeOf<JsonSerializer>());
            Assert.That(Game.Compression, Is.TypeOf<GZipCompression>());
            Assert.That(Game.GetTool<ILogger>(), Is.TypeOf<Logger>());
            Assert.That(Game.Crypto, Is.TypeOf<AesCrypto>());
        }

        [Test]
        public async Task DefaultCrypto_IsStatelessAndBorrowsStreamsAndKeys()
        {
            Game.ConfigureTools(new GameToolConfiguration());
            var key = new byte[64];
            var otherKey = new byte[64];
            new Random(1).NextBytes(key);
            new Random(2).NextBytes(otherKey);
            var originalKey = (byte[])key.Clone();
            var tool = Game.Crypto;
            var data = new byte[] { 1, 2, 3, 4 };
            using var input = new System.IO.MemoryStream(data);
            var encrypted = await tool.EncryptAsync(input, key);
            var otherEncrypted = tool.Encrypt(data, otherKey);
            using var cipher = new System.IO.MemoryStream(encrypted);
            Assert.That(await tool.DecryptAsync(cipher, key), Is.EqualTo(data));
            Assert.That(tool.Decrypt(otherEncrypted, otherKey), Is.EqualTo(data));
            Assert.That(input.CanRead && cipher.CanRead, Is.True);
            Assert.That(key, Is.EqualTo(originalKey));
            Assert.That(Game.Crypto, Is.SameAs(tool));
        }

        [Test]
        public void Configuration_IsASnapshotAndDoesNotConstructTools()
        {
            var configuration = new GameToolConfiguration().Set<IProjectTool, ProjectTool>();
            Game.ConfigureTools(configuration);
            configuration.Set<IProjectTool, OtherProjectTool>();
            Assert.That(s_Created, Is.Zero);
            Assert.That(Game.GetTool<IProjectTool>(), Is.TypeOf<ProjectTool>());
            Assert.Throws<InvalidOperationException>(() => Game.ConfigureTools(configuration));
        }

        [Test]
        public void InvalidTypes_AreRejectedBeforeAnyConstructorRuns()
        {
            var configuration = new GameToolConfiguration().Set<IProjectTool, ProjectTool>();
            configuration.selections.Add(new GameToolConfiguration.Selection
            {
                contract = typeof(ICompression).AssemblyQualifiedName,
                implementation = typeof(string).AssemblyQualifiedName
            });
            Assert.Throws<ArgumentException>(() => new GameToolRegistry(configuration));
            Assert.That(s_Created, Is.Zero);
            Assert.That(GameToolConfiguration.IsSupported(typeof(PrivateTool), typeof(IProjectTool)), Is.False);
            Assert.That(GameToolConfiguration.IsSupported(typeof(GenericTool<>), typeof(IProjectTool)), Is.False);
            Assert.That(GameToolConfiguration.IsContract(typeof(ProjectTool)), Is.False);
            Assert.That(GameToolConfiguration.IsContract(typeof(IGameTool)), Is.False);
        }

        [Test]
        public void LifecycleTypes_CannotBeRegisteredAsTools()
        {
            var configuration = new GameToolConfiguration();
            Assert.Throws<ArgumentException>(() => configuration.Set<IProjectTool, DisposableTool>());
            Assert.Throws<ArgumentException>(() => configuration.Set<IProjectTool, AsyncTool>());
            Assert.Throws<ArgumentException>(() => configuration.Set<IProjectTool, ModuleTool>());
            Assert.That(GameToolConfiguration.IsContract(typeof(IDisposableTool)), Is.False);
            Assert.That(GameToolConfiguration.IsContract(typeof(IModuleTool)), Is.False);
            Assert.Throws<InvalidOperationException>(() => new GameToolRegistry().Get<IInvalidDefaultTool>());
        }

        [Test]
        public async Task ConcurrentAccess_ConstructsSelectedAndDefaultInstancesOnlyOnce()
        {
            Game.ConfigureTools(new GameToolConfiguration().Set<IProjectTool, ProjectTool>());
            var tasks = new Task<IProjectTool>[16];
            for (var i = 0; i < tasks.Length; i++) tasks[i] = Task.Run(Game.GetTool<IProjectTool>);
            var instances = await Task.WhenAll(tasks);
            foreach (var instance in instances) Assert.That(instance, Is.SameAs(instances[0]));
            Assert.That(s_Created, Is.EqualTo(1));
            var defaults = new Task<IDefaultProjectTool>[16];
            for (var i = 0; i < defaults.Length; i++) defaults[i] = Task.Run(Game.GetTool<IDefaultProjectTool>);
            var defaultInstances = await Task.WhenAll(defaults);
            foreach (var instance in defaultInstances) Assert.That(instance, Is.SameAs(defaultInstances[0]));
            Assert.That(s_Created, Is.EqualTo(2));
        }

        [Test]
        public void ConfiguredFailure_DoesNotRetryOrUseInterfaceDefault()
        {
            Game.ConfigureTools(new GameToolConfiguration().Set<IDefaultProjectTool, FailingTool>());
            Assert.That(s_Created, Is.Zero);
            Assert.Throws<TargetInvocationException>(() => Game.GetTool<IDefaultProjectTool>());
            Assert.Throws<TargetInvocationException>(() => Game.GetTool<IDefaultProjectTool>());
            Assert.That(s_Created, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => Game.ConfigureTools(new GameToolConfiguration()));
        }

        [Test]
        public void Constructors_CannotResolveOtherTools()
        {
            Game.ConfigureTools(new GameToolConfiguration().Set<IProjectTool, RecursiveTool>());
            var error = Assert.Throws<TargetInvocationException>(() => Game.GetTool<IProjectTool>());
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(Game.Compression, Is.TypeOf<GZipCompression>());
        }

        [Test]
        public void DefaultDeclaration_IsOptionalAndBelongsToTheExactContract()
        {
            var tools = new GameToolRegistry();
            Assert.That(tools.Get<IDefaultProjectTool>(), Is.TypeOf<DefaultProjectTool>());
            Assert.Throws<InvalidOperationException>(() => tools.Get<IProjectTool>());
            Assert.Throws<InvalidOperationException>(() => tools.Get<IDerivedTool>());
        }

        [Test]
        public void DisplayName_DoesNotIdentifyTheContractOrSupplyAnImplementation()
        {
            var named = typeof(INamedTool).GetCustomAttribute<GameToolAttribute>(inherit: false);
            var other = typeof(IDefaultProjectTool).GetCustomAttribute<GameToolAttribute>(inherit: false);
            Assert.That(named.DisplayName, Is.EqualTo(other.DisplayName));
            Assert.That(typeof(IDerivedTool).GetCustomAttribute<GameToolAttribute>(inherit: false), Is.Null);
            Assert.Throws<InvalidOperationException>(() => new GameToolRegistry().Get<INamedTool>());
            var configuration = new GameToolConfiguration().Set<INamedTool, NamedTool>();
            Assert.That(configuration.selections[0].contract, Is.EqualTo(typeof(INamedTool).AssemblyQualifiedName));
            var tools = new GameToolRegistry(configuration);
            Assert.That(tools.Get<INamedTool>(), Is.TypeOf<NamedTool>());
            Assert.That(tools.Get<IDefaultProjectTool>(), Is.TypeOf<DefaultProjectTool>());
        }

        [Test]
        public async Task ModuleShutdown_WaitsForPendingUnloadAndDoesNotEndTools()
        {
            Game.ConfigureTools(new GameToolConfiguration().Set<IProjectTool, ProjectTool>());
            var tool = Game.GetTool<IProjectTool>();
            var handle = Game.CreateModules();
            handle.Modules.Install<WaitingModule>();
            var module = handle.Modules.GetModule<WaitingModule>();
            var unloading = handle.DisposeAsync().AsTask();
            try
            {
                var shutdown = Game.ShutdownModulesAsync();
                Assert.That(shutdown.IsCompleted, Is.False);
                Assert.Throws<InvalidOperationException>(() => Game.CreateModules());
                module.Release.SetResult(true);
                await unloading;
                await shutdown;
                Assert.That(module.IsDisposed, Is.True);
                Assert.That(Game.ShutdownModulesAsync(), Is.SameAs(shutdown));
                Assert.That(Game.GetTool<IProjectTool>(), Is.SameAs(tool));
                Assert.That(Game.Serializer, Is.TypeOf<JsonSerializer>());
            }
            finally
            {
                module.Release.TrySetResult(true);
                await unloading;
            }
        }

        [Test]
        public void ModuleShutdown_CannotWaitOnItsOwnLifecycle()
        {
            using var scope = GameModuleLifecycleScope.Enter();
            Assert.Throws<InvalidOperationException>(() => Game.ShutdownModulesAsync());
        }

        public interface IProjectTool : IGameTool { }
        public interface IDisposableTool : IGameTool, IDisposable { }
        public interface IModuleTool : IGameTool, IGameModule { }
        [GameTool("默认测试工具", typeof(DefaultProjectTool))]
        public interface IDefaultProjectTool : IGameTool { }
        [GameTool("默认测试工具")]
        public interface INamedTool : IGameTool { }
        public class NamedTool : INamedTool { }
        public interface IDerivedTool : IDefaultProjectTool { }
        [GameTool("非法默认工具", typeof(InvalidDefaultTool))]
        public interface IInvalidDefaultTool : IGameTool { }
        public class ProjectTool : IProjectTool
        {
            public ProjectTool() => Interlocked.Increment(ref s_Created);
        }
        public class OtherProjectTool : IProjectTool { }
        private class PrivateTool : ProjectTool { }
        public class GenericTool<T> : ProjectTool { }
        public class DefaultProjectTool : IDefaultProjectTool
        {
            public DefaultProjectTool() => Interlocked.Increment(ref s_Created);
        }
        public class FailingTool : DefaultProjectTool
        {
            public FailingTool() => throw new InvalidOperationException("construction failure");
        }
        public class RecursiveTool : IProjectTool
        {
            public RecursiveTool() => Game.GetTool<IDefaultProjectTool>();
        }
        public class DisposableTool : DisposableObject, IProjectTool
        {
            protected override void OnDispose() { }
        }
        public class InvalidDefaultTool : DisposableObject, IInvalidDefaultTool
        {
            protected override void OnDispose() { }
        }
        public class ModuleTool : GameModule, IProjectTool { }
        public class AsyncTool : IProjectTool, IAsyncDisposable
        {
            public ValueTask DisposeAsync() => default;
        }
        public class WaitingModule : GameModule
        {
            internal readonly TaskCompletionSource<bool> Release = new();
            protected override async ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
            {
                await Release.Task;
                Assert.That(Game.GetTool<IProjectTool>(), Is.TypeOf<ProjectTool>());
            }
        }
    }
}
