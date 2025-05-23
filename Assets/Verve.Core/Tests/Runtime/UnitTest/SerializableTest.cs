namespace Verve.Tests
{
    using Unit;
    using Serializable;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class SerializableTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private SerializableUnit m_SerializableUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<SerializableUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_SerializableUnit);
        }
    }
}