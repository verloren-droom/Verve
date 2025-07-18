namespace Verve.HotFix
{
    /// <summary>
    /// 热更新功能
    /// </summary>
    [System.Serializable]
    public class HotFixFeature : ModularGameFeature
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new GenericHotFixSubmodule());
        }
    }
}