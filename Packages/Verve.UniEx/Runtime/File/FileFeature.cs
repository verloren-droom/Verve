#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    /// <summary>
    /// 文件功能
    /// </summary>
    [System.Serializable]
    public partial class FileFeature : Verve.File.FileFeature
    {
        protected override void OnLoad(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            m_FileSubmodule = new GenericFileSubmodule();
            m_FileSubmodule?.OnModuleLoaded(dependencies);
        }
    }
}

#endif