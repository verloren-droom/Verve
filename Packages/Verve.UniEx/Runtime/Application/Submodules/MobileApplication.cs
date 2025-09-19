#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Application
{
    /// <summary>
    /// 移动设备应用子模块
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(ApplicationGameFeature), Description = "移动设备应用子模块")]
    public class MobileApplication : GenericApplication
    {
        public override void QuitApplication()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}

#endif