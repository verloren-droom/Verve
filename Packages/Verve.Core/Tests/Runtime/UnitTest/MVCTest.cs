namespace Verve.Tests
{
    using MVC;
    using Unit;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class MVCTest
    {
        private MVCFeature m_MVC;
        
        
        [SetUp]
        public void SetUp()
        {
            m_MVC = new MVCFeature();
            ((IGameFeature)m_MVC).Load();
            ((IGameFeature)m_MVC).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_MVC = null;
        }
        
        /// <summary>
        /// 测试 Activity 的 Model 注册与获取
        /// </summary>
        [Test]
        public void ActivityModelRegistration_ShouldWorkCorrectly()
        {
            TestActivity.Instance.RegisterModel<TestModel>();
            
            var model = TestActivity.Instance.GetModel<TestModel>();
            
            Assert.IsNotNull(model);
            Assert.IsTrue(model.IsInitialized);
        }
        
        /// <summary>
        /// 测试 Command 的执行与撤销
        /// </summary>
        [Test]
        public void CommandExecution_ShouldWorkCorrectly()
        {
            var command = TestActivity.Instance.RegisterCommand<TestCommand>();
            
            Assert.IsNotNull(command);
            
            TestActivity.Instance.ExecuteCommand<TestCommand>();

            Assert.IsTrue(command.IsExecuted);

            TestActivity.Instance.UndoCommand<TestCommand>();
            
            Assert.IsFalse(command.IsExecuted);
        }

        /// <summary>
        /// 测试 Controller 的 Model 访问
        /// </summary>
        [Test]
        public void ControllerModelAccess_ShouldWorkCorrectly()
        {
            TestActivity.Instance.RegisterModel<TestModel>();
            
            var controller = new TestController { Activity = TestActivity.Instance };
            var model = controller.GetModel<TestModel>();
            
            Assert.IsNotNull(model);
        }
        
        /// <summary>
        /// 测试 View 的生命周期
        /// </summary>
        [Test]
        public void ViewLifecycle_ShouldWorkCorrectly()
        {
            var view = new TestView { Activity = TestActivity.Instance };
            
            bool openedFired = false;
            bool closedFired = false;
            
            view.OnOpened += v => openedFired = true;
            view.OnClosed += v => closedFired = true;

            ((IView)view).Open();
            Assert.IsTrue(openedFired);
            
            view.Close();
            Assert.IsTrue(closedFired);
        }
        
        
        private class TestModel : ModelBase
        {
            public bool IsInitialized { get; private set; }
            
            protected override void OnInitialized()
            {
                IsInitialized = true;
            }
        }

        
        private class TestCommand : CommandBase
        {
            public bool IsExecuted { get; private set; }
            
            protected override void OnExecute() => IsExecuted = true;
            protected override void OnUndo() => IsExecuted = false;
        }

        
        private class TestController : IController
        {
            public IActivity Activity { get; set; }
        }

        private class TestView : ViewBase
        {
            public override string ViewName { get; } = nameof(TestView);
            public override IActivity Activity { get; set; }
        }

        private class TestActivity : ActivityBase<TestActivity>
        {
            protected override void OnInitialized()
            {
                
            }
        }
    }
}