using Verve.Debugger;
using Verve.Unit;

namespace Verve.Example
{
    using ECS;
    using MVC;
    using Input;
    using Loader;
    
    
    public class VerveExample : InstanceBase<VerveExample>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            GameLauncher.Instance.AddUnit<ECSUnit>();
            GameLauncher.Instance.AddUnit<MVCUnit>();
            GameLauncher.Instance.AddUnit<InputUnit>();
            GameLauncher.Instance.AddUnit<LoaderUnit>();
            GameLauncher.Instance.AddUnit<DebuggerUnit>();
        }

        public void Initialize()
        {
            GameLauncher.Instance.Initialize();
        }
    }
}