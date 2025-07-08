#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    /// <summary>
    /// WebGL应用子模块
    /// </summary>
    [System.Serializable]
    public class WebGLApplicationSubmodule : GenericApplicationSubmodule
    {
        public override string ModuleName => "WebGLApplication";
        // [System.Runtime.InteropServices.DllImport("__Internal")]
        // private static extern void ReloadPage();

        public override void QuitApplication()
        {
            UnityEngine.Application.OpenURL("about:blank");
        }

        public override void RestartApplication()
        {
            UnityEngine.Application.ExternalEval("window.location.reload();");
        }
    }
}

#endif