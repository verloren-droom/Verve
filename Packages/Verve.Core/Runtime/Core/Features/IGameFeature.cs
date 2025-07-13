namespace Verve
{
    /// <summary>
    /// 游戏功能接口
    /// </summary>
    public interface IGameFeature
    {
        /// <summary> 加载 </summary>
        void Load(IReadOnlyFeatureDependencies dependencies);
        /// <summary> 激活 </summary>
        void Activate();
        /// <summary> 停用 </summary>
        void Deactivate();
        /// <summary> 卸载 </summary>
        void Unload();
    }
}