namespace Verve.Tests
{
    using Unit;
    using Debugger;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class DebuggerTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private DebuggerUnit m_DebuggerUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<DebuggerUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_DebuggerUnit);
        }
                
        [TearDown]
        public void Teardown()
        {
            m_DebuggerUnit = null;
        }
        
        [Test]
        public void LogLevelAndMessageTracking_ShouldWorkCorrectly()
        {
            m_DebuggerUnit.Log("Test Log");
            
            Assert.AreEqual("Test Log", m_DebuggerUnit.GetLastLog().Message);
            Assert.AreEqual(LogLevel.Log, m_DebuggerUnit.GetLastLog().Level);
            
            m_DebuggerUnit.LogWarning("Test Warning");
            
            Assert.AreEqual("Test Warning", m_DebuggerUnit.GetLastLog().Message);
            Assert.AreEqual(LogLevel.Warning, m_DebuggerUnit.GetLastLog().Level);
            
            m_DebuggerUnit.LogError("Test Error");
            
            Assert.AreEqual("Test Error", m_DebuggerUnit.GetLastLog().Message);
            Assert.AreEqual(LogLevel.Error, m_DebuggerUnit.GetLastLog().Level);
        }
        
        [Test]
        public void DisableDebugger_ShouldWorkCorrectly()
        {
            const string testMessage = "Should not be logged";
            
            m_DebuggerUnit.IsEnable = false;
            m_DebuggerUnit.Log(testMessage);
            
            Assert.AreNotEqual(testMessage, m_DebuggerUnit.GetLastLog().Message);
        }
        
        [Test]
        public void LogFormatting_ShouldWorkCorrectly()
        {
            m_DebuggerUnit.Log("Formatted {0} {1}", "log", 1);
            
            Assert.AreEqual("Formatted log 1", m_DebuggerUnit.GetLastLog().Message);
        }
    }
}