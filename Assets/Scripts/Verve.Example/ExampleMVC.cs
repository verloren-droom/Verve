namespace Verve.Example
{
#if VERVE_FRAMEWORK_EVENT
    using Event;
#endif
#if VERVE_FRAMEWORK_MVC
    using MVC;
    
    
    public class ExampleProcedure : Procedure<ExampleProcedure>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            RegisterModel<ExampleModel>();
        }
    }

    public class ExampleModel : ModelBase
    {
        public PropertyProxy<int> Value = new PropertyProxy<int>(5);

        public override void Initialize()
        {
            
        }
    }

    // public class ExampleView : ViewBase, IController
    // {
    //
    // }
#endif
}