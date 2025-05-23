namespace Verve.Tests
{
    using Unit;
    using MVC;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class MVCTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private MVCUnit m_MVCUnit;
        private TestActivity m_TestActivity;
        
        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<MVCUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_MVCUnit);
            
            m_TestActivity = new TestActivity();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_MVCUnit = null;
            m_TestActivity = null;
        }
        
        /// <summary>
        /// 测试 Activity 的 Model 注册与获取
        /// </summary>
        [Test]
        public void ActivityModelRegistration_ShouldWorkCorrectly()
        {
            m_TestActivity.RegisterModel<TestModel>();
            
            var model = m_TestActivity.GetModel<TestModel>();
            
            Assert.IsNotNull(model);
            Assert.IsTrue(model.IsInitialized);
        }
        
        /// <summary>
        /// 测试 Command 的执行与撤销
        /// </summary>
        [Test]
        public void CommandExecution_ShouldWorkCorrectly()
        {
            var command = m_TestActivity.RegisterCommand<TestCommand>();
            
            Assert.IsNotNull(command);
            
            m_TestActivity.ExecuteCommand<TestCommand>();

            Assert.IsTrue(command.IsExecuted);

            m_TestActivity.UndoCommand<TestCommand>();
            
            Assert.IsFalse(command.IsExecuted);
        }

        /// <summary>
        /// 测试 Controller 的 Model 访问
        /// </summary>
        [Test]
        public void ControllerModelAccess_ShouldWorkCorrectly()
        {
            m_TestActivity.RegisterModel<TestModel>();
            
            var controller = new TestController { Activity = m_TestActivity };
            var model = controller.GetModel<TestModel>();
            
            Assert.IsNotNull(model);
        }
        
        /// <summary>
        /// 测试 View 的生命周期
        /// </summary>
        [Test]
        public void ViewLifecycle_ShouldWorkCorrectly()
        {
            var view = new TestView { Activity = m_TestActivity };
            
            bool openedFired = false;
            bool closedFired = false;
            
            view.OnOpened += v => openedFired = true;
            view.OnClosed += v => closedFired = true;
            
            view.Open();
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