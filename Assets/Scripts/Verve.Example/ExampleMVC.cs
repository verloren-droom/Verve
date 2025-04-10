using System;

namespace Verve.Example
{
#if VERVE_FRAMEWORK_EVENT
    using Event;
#endif
#if VERVE_FRAMEWORK_MVC
    using MVC;
    
    
    public class ExampleActivity : ActivityBase<ExampleActivity>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            RegisterModel<ExampleModel>();
        }
    }

    public class ExampleModel : ModelBase
    {
        public PropertyProxy<int> Value = new PropertyProxy<int>(5545);
    }

    public class ExampleMVC : ViewBase, IController
    {
        public override IActivity Activity { get; set; } = ExampleActivity.Instance;

        private void Awake()
        {
            this.GetModel<ExampleModel>().Value.PropertyChanged += (_, _) =>
            { 
                UnityEngine.Debug.Log("MVC ->" + this.GetModel<ExampleModel>()?.Value);
            };
            this.GetModel<ExampleModel>().Value.Value += 10;
        }

        public void Deinitialize()
        {
            
        }
    }
#endif
}