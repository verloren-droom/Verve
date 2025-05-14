namespace Verve.Sample
{
    
    
    using ECS;
    using MVC;
    using Input;
    using Loader;
    using Debugger;
    
    
    public class VerveExample : InstanceBase<VerveExample>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Launcher.Instance.AddUnit<ECSUnit>();
            Launcher.Instance.AddUnit<MVCUnit>();
            Launcher.Instance.AddUnit<InputUnit>();
            Launcher.Instance.AddUnit<LoaderUnit>();
            Launcher.Instance.AddUnit<DebuggerUnit>();
        }

        public void Initialize()
        {
            Launcher.Instance.Initialize();
        }
    }
}