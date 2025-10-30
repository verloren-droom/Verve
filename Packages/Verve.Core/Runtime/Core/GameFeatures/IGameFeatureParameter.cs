namespace Verve
{
    /// <summary>
    ///   <para>游戏功能参数接口</para>
    ///   <para>用于配置游戏功能的参数</para>
    /// </summary>
    public interface IGameFeatureParameter
    {
        /// <summary>
        ///   <para>获取参数值</para>
        /// </summary>
        /// <typeparam name="T">参数值类型</typeparam>
        /// <returns>
        ///   <para>参数值</para>
        /// </returns>
        T GetValue<T>();
        
        /// <summary>
        ///   <para>设置参数值</para>
        /// </summary>
        void SetValue(IGameFeatureParameter parameter);
        
        /// <summary>
        ///   <para>参数是否被覆盖</para>
        /// </summary>
        bool Overrides { get; set; }
        
        /// <summary>
        ///   <para>应用参数覆盖</para>
        /// </summary>
        /// <param name="other">其他参数</param>
        void ApplyOverride(IGameFeatureParameter other);
    }
}