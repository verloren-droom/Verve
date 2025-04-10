
namespace Verve.Example
{
    using ECS;
    using MVC;
    
    public class VerveExample : InstanceBase<VerveExample>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            GameLauncher.Instance.AddUnit<ECSUnit>();
            GameLauncher.Instance.AddUnit<MVCUnit>();
        }

        public void Initialize()
        {
            GameLauncher.Instance.Initialize();
        }
    }
}