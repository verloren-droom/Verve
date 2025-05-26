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
            float triggeredResult = 1.0f;
            const string mapName = "TestMap";
            const string actionName = "TestValue";
    
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap(mapName);
            var action = map.AddAction(actionName, InputActionType.Value);
            
            var obj = new GameObject("input");
            var playerInput = obj.AddComponent<PlayerInput>();
            playerInput.actions = asset;
            
            m_UnitRules.AddDependency<InputUnit>(playerInput);
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_InputUnit);
            
            m_InputUnit.Enable<InputSystemService>();
            
            m_InputUnit.AddListener<InputSystemService, float>($"{mapName}/{actionName}", ctx => {
                isTriggered = true;
                triggeredResult = ctx.value;
            });

            m_InputUnit.SimulateInputAction<InputManagerService, float>($"{mapName}/{actionName}", -1.0f);

            Assert.IsTrue(isTriggered);
            Assert.AreEqual(-1.0f, triggeredResult);
            
            Object.DestroyImmediate(playerInput.gameObject);
        }
        
        [Test]
        public void SimulateInputManager_ShouldWorkCorrectly()
        {
            bool isTriggered = false;
            float triggeredResult = 1.0f;
            const string mapName = "TestMap";
            const string actionName = "TestValue";

            m_UnitRules.AddDependency<InputUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_InputUnit);
            m_InputUnit.Enable<InputManagerService>();
            m_InputUnit.AddListener<InputManagerService, float>($"{mapName}/{actionName}", ctx =>
            {
                isTriggered = true;
                triggeredResult = ctx.value;
            });
            
            m_InputUnit.SimulateInputAction<InputManagerService, float>($"{mapName}/{actionName}", -1.0f);

            Assert.IsTrue(isTriggered);
            Assert.AreEqual(-1.0f, triggeredResult);
        }
    }
}

#endif