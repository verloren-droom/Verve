using System;
using System.Collections.Generic;

namespace Verve
{
    /// <summary>
    /// 功能状态
    /// </summary>
    [System.Serializable]
    public enum FeatureState
    {
        /// <summary> 未加载 </summary>
        Unloaded,
        /// <summary> 加载完成 </summary>
        Loaded,
        /// <summary> 已激活 </summary>
        Active,
        /// <summary> 卸载中 </summary>
        Unloading
    }
    
    
    /// <summary>
    /// 游戏功能系统
    /// </summary>
    public static class GameFeaturesSystem
    {
        private static readonly GameFeaturesRuntime s_Runtime = new GameFeaturesRuntime();
        public static GameFeaturesRuntime Runtime => s_Runtime;
    }


    /// <summary>
    /// 游戏功能子模块接口
    /// </summary>
    public interface IGameFeatureSubmodule
    {
        string ModuleName { get; }
        void OnModuleLoaded();
        void OnModuleUnloaded();
    }
}