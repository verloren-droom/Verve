#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.HotFix
{
    /// <summary>
    /// 热更功能模块 - 负责脚本热更，管理脚本的下载、校验、加载、重定向等
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/HotFix", Description = "热更功能模块 - 负责脚本热更，管理脚本的下载、校验、加载、重定向等")]
    internal sealed class HotFixGameFeature : GameFeatureModule
    {
        
    }
}

#endif