#if UNITY_5_3_OR_NEWER && UNITY_WEBGL

namespace VerveUniEx.Application
{
    /// <summary>
    /// WebGL应用子模块
    /// </summary>
    public class WebGLApplicationSubmodule : GenericApplicationSubmodule
    {
        public override void QuitApplication()
        {
            UnityEngine.Application.OpenURL("about:blank");
        }
    }
}

#endif