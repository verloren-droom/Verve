namespace Verve.MVC
{
    using Unit;
    using Loader;
    

    /// <summary>
    /// MVC 单元
    /// </summary>
    [CustomUnit("MVC", dependencyUnits: typeof(LoaderUnit)), System.Serializable]
    public partial class MVCUnit : UnitBase
    {
        protected LoaderUnit m_LoaderUnit;

        
        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            parent.TryGetDependency<LoaderUnit>(out m_LoaderUnit);
        }
    }
}