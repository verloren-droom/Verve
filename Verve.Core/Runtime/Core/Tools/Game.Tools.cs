namespace Verve
{
    using System;

    /// <summary>
    ///   <para>游戏入口；全局工具访问与实现配置。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>配置锁。</para>
        /// </summary>
        private static readonly object s_ToolsLock = new();
        /// <summary>
        ///   <para>工具注册表；脚本域内固定，不随运行模式重建。</para>
        /// </summary>
        private static volatile GameToolRegistry s_Tools;

        /// <summary>
        ///   <para>全局工具；首次访问读取项目配置。</para>
        /// </summary>
        private static GameToolRegistry Tools
        {
            get
            {
                if (s_Tools != null) return s_Tools;
                lock (s_ToolsLock)
                    return s_Tools ??= new GameToolRegistry(
#if UNITY_5_3_OR_NEWER
                        LoadToolConfiguration()
#endif
                    );
            }
        }

        /// <summary>
        ///   <para>获取全局共享工具。</para>
        /// </summary>
        /// <typeparam name="T">继承 <see cref="IGameTool"/> 的能力接口。</typeparam>
        public static T GetTool<T>() where T : class, IGameTool => Tools.Get<T>();

        /// <summary>
        ///   <para>首次使用前固定实现映射；不构造工具，后续不可替换。</para>
        /// </summary>
        /// <param name="configuration">项目实现配置。</param>
        /// <remarks>非 Unity 宿主的配置入口；Unity 自动读取 Project Settings。</remarks>
        public static void ConfigureTools(GameToolConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            lock (s_ToolsLock)
            {
                if (s_Tools != null) throw new InvalidOperationException("Tool configuration is already fixed for this domain.");
                s_Tools = new GameToolRegistry(configuration);
            }
        }
    }
}