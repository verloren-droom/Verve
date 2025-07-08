#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    /// <summary>
    /// Window应用程序子模块
    /// </summary>
    [System.Serializable]
    public class WindowApplicationSubmodule : GenericApplicationSubmodule
    {
        public override string ModuleName => "WindowApplication";
    }
}

#endif