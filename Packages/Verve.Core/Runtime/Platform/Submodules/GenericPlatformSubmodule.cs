namespace Verve.Platform
{
    /// <summary>
    /// 通用平台子模块
    /// </summary>
    public class GenericPlatformSubmodule : PlatformSubmodule
    {
        public override string ModuleName => "GenericPlatform";
        
        public override string PlatformName { get; }
    }
}