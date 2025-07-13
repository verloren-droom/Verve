#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    /// <summary>
    /// Unity通用文件子模块
    /// </summary>
    public sealed partial class GenericFileSubmodule : Verve.File.FileSubmodule
    {
        public override string ModuleName => "GenericFile.UniEx";
    }
}

#endif