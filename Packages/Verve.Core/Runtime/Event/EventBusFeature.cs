namespace Verve.Event
{
    /// <summary>
    /// 事件总线功能功能
    /// </summary>
    [System.Serializable]
    public class EventBusFeature : ModularGameFeature
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnAfterSubmodulesLoaded(dependencies);
            RegisterSubmodule(new EnumEventBusSubmodule());
            RegisterSubmodule(new StringEventBusSubmodule());
            RegisterSubmodule(new TypeEventBusSubmodule());
        }
    }
}