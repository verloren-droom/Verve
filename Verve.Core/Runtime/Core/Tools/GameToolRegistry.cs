namespace Verve
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>工具注册表；固定实现映射并按需缓存实例。</para>
    /// </summary>
    internal sealed class GameToolRegistry
    {
        /// <summary>
        ///   <para>构造标记；阻止工具在构造期间获取其他工具。</para>
        /// </summary>
        [ThreadStatic] private static bool s_Constructing;
        /// <summary>
        ///   <para>实现映射快照。</para>
        /// </summary>
        private readonly Dictionary<Type, Type> m_Selections;
        /// <summary>
        ///   <para>实例缓存；保留构造错误，不重试或改用默认实现。</para>
        /// </summary>
        private readonly Dictionary<Type, Lazy<IGameTool>> m_Tools = new();

        /// <summary>
        ///   <para>保存配置快照；不构造工具。</para>
        /// </summary>
        /// <param name="configuration">实现选择；空值使用接口默认声明。</param>
        internal GameToolRegistry(GameToolConfiguration configuration = null) =>
            m_Selections = configuration?.GetSelections() ?? new Dictionary<Type, Type>();

        /// <summary>
        ///   <para>取得共享工具；同一接口只构造一次。</para>
        /// </summary>
        /// <typeparam name="T">工具能力接口。</typeparam>
        internal T Get<T>() where T : class, IGameTool
        {
            if (s_Constructing) throw new InvalidOperationException("Tool constructors must not resolve tools. Compose operations inside methods instead.");
            lock (m_Tools)
            {
                var contract = typeof(T);
                if (!m_Tools.TryGetValue(contract, out var tool))
                {
                    tool = new Lazy<IGameTool>(() =>
                    {
                        if (!GameToolConfiguration.IsContract(contract))
                            throw new ArgumentException($"'{contract}' must be a public tool interface without a lifecycle.");
                        var implementation = m_Selections.TryGetValue(contract, out var selected) ? selected
                            : contract.GetCustomAttribute<GameToolAttribute>(inherit: false)?.DefaultImplementationType;
                        if (implementation == null)
                            throw new InvalidOperationException($"No implementation is configured for {contract.FullName}. Select one in Project Settings > Verve > Tools or call Game.ConfigureTools before first use.");
                        if (!GameToolConfiguration.IsSupported(implementation, contract, allowInternal: true))
                            throw new InvalidOperationException($"Invalid implementation '{implementation}' for {contract.FullName}. Tools must not own a lifecycle or persistent resources.");
                        s_Constructing = true;
                        try { return (IGameTool)Activator.CreateInstance(implementation); }
                        finally { s_Constructing = false; }
                    });
                    m_Tools.Add(contract, tool);
                }
                return (T)tool.Value;
            }
        }
    }
}