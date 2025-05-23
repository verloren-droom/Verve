using Verve.Timer;
using VerveUniEx.Storage;

namespace VerveUniEx.Example
{
    
    using Verve.File;
    using Verve.Serializable;
    using Verve.MVC;
    using Verve.Event;
    using UnityEngine;
    using UnityEngine.UI;
    using ViewBase = MVC.ViewBase;
    using ProtoBuf;
    using System.IO;


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
            
            GameLauncher.Instance.Debugger.Log("TEST");
            StartCoroutine(GameLauncher.Instance.Storage.WriteAsync<BuiltInStorage, TestData>("Test1",
                new TestData() { v1 = -100, v2 = "TEST1" }, () =>
                {
                    GameLauncher.Instance.Debugger.Log("Write");
                    StartCoroutine(GameLauncher.Instance.Storage.ReadAsync<BuiltInStorage, TestData>("Test1", (value) =>
                    {
                        GameLauncher.Instance.Debugger.Log($"{value.v1} {value.v2}");
                    }));
                }));
            
            
            GameLauncher.Instance.Timer.AddTimer<SimpleTimerService>(2 * 1000, () => { Debug.Log("2 TIMEOUT"); });
        }

        [ProtoContract]
        public class TestData
        {
            [ProtoMember(1)] public int v1 { get; set; }
            [ProtoMember(2)] public string v2 { get; set; }
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