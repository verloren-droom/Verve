#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Loader
{
    /// <summary>
    ///   <para>加载器游戏功能</para>
    ///   <para>负责所有资源（美术、音频、配置等）的本地和远程加载、热更、缓存、卸载等</para>
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/Loader", Description = "加载器游戏功能 - 负责所有资源（美术、音频、配置等）的本地和远程加载、热更、缓存、卸载等")]
    internal sealed class LoaderGameFeature : GameFeatureModule
    {
        
    }
}

#endif