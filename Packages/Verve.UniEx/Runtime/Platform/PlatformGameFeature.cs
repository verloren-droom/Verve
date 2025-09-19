namespace Verve.UniEx.Platform
{
    /// <summary>
    /// 平台游戏功能模块 - 负责平台相关操作
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/Platform", Description = "平台游戏功能模块 - 负责平台相关操作", SelectionMode = SubmoduleSelectionMode.Locked)]
    internal sealed class PlatformGameFeature : GameFeatureModule
    {
        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            Add<GenericPlatform>();
#elif UNITY_STANDALONE_WIN
            
#elif UNITY_STANDALONE_OSX
            Add<MacPlatform>();
#elif UNITY_WEBGL
            
#else
            Add<GenericApplication>();
#endif
            // Add<MobilePlatform>();
            // Add<AndroidPlatform>();
        }
    }
}