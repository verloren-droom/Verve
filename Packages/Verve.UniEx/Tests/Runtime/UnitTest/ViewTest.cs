#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using MVC;
    using Verve.Unit;
    using UnityEngine;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class ViewTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private MVCUnit m_ViewUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<MVCUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_ViewUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_ViewUnit = null;
        }
    }
}

#endif