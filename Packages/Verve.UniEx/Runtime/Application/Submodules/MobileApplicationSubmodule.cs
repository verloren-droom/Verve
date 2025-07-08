#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    /// <summary>
    /// 移动设备应用子模块
    /// </summary>
    [System.Serializable]
    public class MobileApplicationSubmodule : GenericApplicationSubmodule
    {
        public override string ModuleName => "MobileApplication";

        public override void QuitApplication()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}

#endif