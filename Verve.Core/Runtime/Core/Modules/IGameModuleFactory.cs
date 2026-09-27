namespace Verve
{
    using System;

    /// <summary>
    ///   <para>模块工厂；创建和配置模块，实例交由容器释放。</para>
    /// </summary>
    public interface IGameModuleFactory : IDisposable
    {
        /// <summary>
        ///   <para>创建指定精确类型的全新模块。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        GameModule Create(Type moduleType);

        /// <summary>
        ///   <para>配置已接管的模块；先于清单条目配置和安装。</para>
        /// </summary>
        /// <param name="module">已由容器接管的模块。</param>
        void Configure(GameModule module);
    }

    /// <summary>
    ///   <para>默认模块工厂；调用公开无参构造函数。</para>
    /// </summary>
    internal sealed class GameModuleFactory : DisposableObject, IGameModuleFactory
    {
        /// <inheritdoc />
        public GameModule Create(Type moduleType)
        {
            var error = GameModuleUtility.GetModuleTypeError(moduleType);
            if (error != null) throw new ArgumentException(error, nameof(moduleType));
            return (GameModule)Activator.CreateInstance(moduleType);
        }

        /// <inheritdoc />
        public void Configure(GameModule module) { }
    }
}