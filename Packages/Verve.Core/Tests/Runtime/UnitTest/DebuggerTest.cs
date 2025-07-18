namespace Verve.Tests
{
    using Unit;
    using Debug;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class DebuggerTest
    {
        private DebuggerFeature m_Debugger;
        

        
        [SetUp]
        public void SetUp()
        {
            m_Debugger = new DebuggerFeature();
            ((IGameFeature)m_Debugger).Load(null);
            ((IGameFeature)m_Debugger).Activate();
        }
                
        [TearDown]
        public void Teardown()
        {
            m_Debugger = null;
        }
        
        [Test]
        public void LogLevelAndMessageTracking_ShouldWorkCorrectly()
        {
            m_Debugger.Log("Test Log");
            
            Assert.AreEqual("Test Log", m_Debugger.GetLastLog().Message);
            Assert.AreEqual(LogLevel.Log, m_Debugger.GetLastLog().Level);
            
            m_Debugger.LogWarning("Test Warning");
            
            Assert.AreEqual("Test Warning", m_Debugger.GetLastLog().Message);
            Assert.AreEqual(LogLevel.Warning, m_Debugger.GetLastLog().Level);
            
            m_Debugger.LogError("Test Error");
            
            Assert.AreEqual("Test Error", m_Debugger.GetLastLog().Message);
            Assert.AreEqual(LogLevel.Error, m_Debugger.GetLastLog().Level);
        }
        
        [Test]
        public void DisableDebugger_ShouldWorkCorrectly()
        {
            const string testMessage = "Should not be logged";

            ((IGameFeature)m_Debugger).Deactivate();
            m_Debugger.Log(testMessage);
            
            Assert.AreNotEqual(testMessage, m_Debugger.GetLastLog().Message);
        }
        
        [Test]
        public void LogFormatting_ShouldWorkCorrectly()
        {
            m_Debugger.Log("Formatted {0} {1}", "log", 1);
            
            Assert.AreEqual("Formatted log 1", m_Debugger.GetLastLog().Message);
        }
    }
}