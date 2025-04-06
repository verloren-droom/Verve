namespace Verve.ECS
{
    using Unit;
    
    
    [CustomUnit("ECS")]
    public class ECSUnit : UnitBase
    {
        public override void Startup(UnitRules parent, params object[] args)
        {
            base.Startup(parent, args);
            ECSWorld.Instance.Initialize();
        }
    }
}