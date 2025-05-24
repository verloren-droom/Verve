#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using Input;
    using Verve.Unit;
    using UnityEngine;
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
        }
        
        [TearDown]
        public void Teardown()
        {
            m_InputUnit = null;
        }
        
        [Test]
        public void SimulateInputSystem_ShouldWorkCorrectly()
        {
            bool isTriggered = false;
            const string mapName = "TestMap";
            const string actionName = "Fire";
    
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap(mapName);
            var action = map.AddAction(actionName, InputActionType.Button);
            
            var obj = new GameObject("input");
            var playerInput = obj.AddComponent<PlayerInput>();
            playerInput.actions = asset;
            
            m_UnitRules.AddDependency<InputUnit>(playerInput);
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_InputUnit);
            
            m_InputUnit.Enable<InputSystemService>();
            
            m_InputUnit.AddListener<InputSystemService, bool>($"{mapName}/{actionName}", ctx => {
                isTriggered = true;
            });

            var keyboard = InputSystem.AddDevice<Keyboard>();
            action.ApplyBindingOverride("<Keyboard>/space");
            
            
            InputSystem.Update();
            
            Assert.IsTrue(isTriggered);
            
            InputSystem.RemoveDevice(keyboard);
            Object.DestroyImmediate(playerInput.gameObject);
        }
    }

}

#endif