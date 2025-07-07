#if UNITY_5_3_OR_NEWER && (UNITY_ANDROID || UNITY_IOS)

namespace VerveUniEx.Application
{
    /// <summary>
    /// 移动设备应用子模块
    /// </summary>
    public class MobileApplicationSubmodule : GenericApplicationSubmodule
    {
        public override void QuitApplication()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}

#endif