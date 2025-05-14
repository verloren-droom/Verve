namespace Verve.ECS
{
    using Unit;
    
    
    [CustomUnit("ECS")]
    public class ECSUnit : UnitBase
    {
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
            ECSWorld.Instance.Initialize();
        }
    }
}