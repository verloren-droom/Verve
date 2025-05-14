namespace VerveUniEx.Example
{
    
    using Verve.MVC;
    using Verve.Event;
    using UnityEngine;
    using UnityEngine.UI;
    using ViewBase = MVC.ViewBase;
    using Launcher = VerveUniEx.Launcher;

    
    public class ExampleActivity : ActivityBase<ExampleActivity>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            RegisterModel<ExampleModel>();
            RegisterModel<VirtualJoystickModel>();
        }
    }

    public class ExampleModel : ModelBase
    {
        public PropertyProxy<int> Value = new PropertyProxy<int>(1);
    }

    public class ExampleMVC : ViewBase, IController
    {
        [SerializeField] private Button m_AddBtn = null;
        [SerializeField] private Button m_SubBtn = null;
        [SerializeField] private Text m_DisplayText = null;

        public override IActivity Activity { get; set; } = ExampleActivity.Instance;

        private void Awake()
        {
            m_DisplayText.text = $"{this.GetModel<ExampleModel>().Value.Value}";
            this.GetModel<ExampleModel>().Value.PropertyChanged += (sender, _) =>
            {
                m_DisplayText.text = $"{this.GetModel<ExampleModel>().Value.Value}";
            };
            
            m_AddBtn?.onClick.AddListener(OnClickAdd);
            m_SubBtn?.onClick.AddListener(OnClickSub);
            
            Launcher.Instance.AddUnit<VerveUniEx.Debugger.DebuggerUnit>();
            Launcher.Instance.Initialize();
            Launcher.Instance.Debugger.Log("DEBUG-DEBUG");
        }

        private void Start()
        {
        }

        void OnClickAdd()
        {
            this.ExecuteCommand<ExampleChangeCommand>();
        }

        void OnClickSub()
        {
            this.UndoCommand<ExampleChangeCommand>();
        }
    }

    public class ExampleChangeCommand : CommandBase
    {
        private ExampleModel m_Model;
        protected override void OnExecute()
        {
            m_Model ??= this.GetModel<ExampleModel>();
            m_Model.Value.Value++;
        }

        protected override void OnUndo()
        {
            m_Model ??= this.GetModel<ExampleModel>();
            m_Model.Value.Value--;
        }
    }
    
}