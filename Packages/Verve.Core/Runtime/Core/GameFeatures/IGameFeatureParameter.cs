namespace Verve
{
    /// <summary>
    /// 游戏功能参数接口 - 用于配置游戏功能的参数
    /// </summary>
    public interface IGameFeatureParameter
    {
        /// <summary> 获取参数值 </summary>
        T GetValue<T>();
        /// <summary> 设置参数值 </summary>
        void SetValue(IGameFeatureParameter parameter);
        /// <summary> 是否覆盖 </summary>
        bool Overrides { get; set; }
        /// <summary> 应用参数覆盖 </summary>
        /// <param name="other"></param>
        void ApplyOverride(IGameFeatureParameter other);
    }
}