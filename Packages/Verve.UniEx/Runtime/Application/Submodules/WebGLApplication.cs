#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Application
{
    /// <summary>
    /// WebGL应用子模块
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(ApplicationGameFeature), Description = "WebGL应用子模块")]
    public class WebGLApplication : GenericApplication
    {
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