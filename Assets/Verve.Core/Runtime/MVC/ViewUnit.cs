namespace Verve.MVC
{
    using Unit;
    using Loader;


    /// <summary>
    /// 视图单元
    /// </summary>
    [CustomUnit("View", dependencyUnits: typeof(LoaderUnit)), System.Serializable]
    public partial class ViewUnit : UnitBase
    {
        protected LoaderUnit m_LoaderUnit;

        
        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            parent.TryGetDependency<LoaderUnit>(out m_LoaderUnit);
        }
    }
}