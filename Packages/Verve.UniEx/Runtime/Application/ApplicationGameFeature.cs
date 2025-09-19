#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Application
{
    /// <summary>
    /// 应用游戏功能模块 - 负责应用程序的生命周期、版本号、设备信息等
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/Application", Description = "应用游戏功能模块 - 负责应用程序的生命周期、版本号、设备信息等", SelectionMode = SubmoduleSelectionMode.Locked)]
    internal sealed class ApplicationGameFeature : GameFeatureModule
    {
        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            Add<GenericApplication>();
#elif UNITY_STANDALONE_WIN
            Add<WindowApplication>();
#elif UNITY_STANDALONE_OSX
            Add<MacApplication>();
#elif UNITY_WEBGL
            Add<WebGLApplication>();
#else
            Add<GenericApplication>();
#endif
        }
    }
}

#endif