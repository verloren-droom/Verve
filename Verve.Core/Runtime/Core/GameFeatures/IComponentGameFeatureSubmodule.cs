namespace Verve
{
    /// <summary>
    ///   <para>游戏功能子模块组件接口</para>
    ///   <para>用于绑定具体的游戏功能组件</para>
    /// </summary>
    public interface IComponentGameFeatureSubmodule : IGameFeatureSubmodule
    {
        /// <summary>
        ///   <para>组件类型</para>
        /// </summary>
        System.Type ComponentType { get; }
        
        /// <summary>
        ///   <para>获取组件</para>
        /// </summary>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        IGameFeatureComponent GetComponent();
        
        /// <summary>
        ///   <para>设置组件</para>
        /// </summary>
        /// <param name="component">组件实例</param>
        void SetComponent(IGameFeatureComponent component);
    }
}