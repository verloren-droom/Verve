namespace VerveUniEx.Tests
{
    using Input;
    using Verve.Unit;
    using NUnit.Framework;
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    
    [TestFixture]
    public class InputUniExTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private InputUnit m_InputUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<InputUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_InputUnit);
        }
        
        [Test]
        public void InputServiceInitialization_ShouldWorkCorrectly()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Assert.IsNotNull(m_InputUnit.GetService<InputManagerService>());
#endif
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
            Assert.IsNotNull(m_InputUnit.GetService<InputSystemService>());
#endif
        }

    }
}