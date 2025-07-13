namespace Verve
{
    /// <summary>
    /// 游戏功能基类
    /// </summary>
    [System.Serializable]
    public abstract class GameFeature : IGameFeature
    {
        void IGameFeature.Load(IReadOnlyFeatureDependencies dependencies) => OnLoad(dependencies);
        void IGameFeature.Activate() => OnActivate();
        void IGameFeature.Deactivate() => OnDeactivate();
        void IGameFeature.Unload() => OnUnload();

        /// <summary> 在模块加载时调用 </summary>
        protected virtual void OnLoad(IReadOnlyFeatureDependencies dependencies) { }
        /// <summary> 在模块注销时调用 </summary>
        protected virtual void OnActivate() { }
        /// <summary> 在模块注销时调用 </summary>
        protected virtual void OnDeactivate() { }
        /// <summary> 在模块注销时调用 </summary>
        protected virtual void OnUnload() { }
    }
}