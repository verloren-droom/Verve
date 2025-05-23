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
    }
}