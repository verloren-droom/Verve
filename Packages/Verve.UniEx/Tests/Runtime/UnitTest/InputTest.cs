#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using Verve;
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
        private InputFeature m_Input;

        
        [SetUp]
        public void SetUp()
        {
            m_Input = new InputFeature();
            ((IGameFeature)m_Input).Load(null);
            ((IGameFeature)m_Input).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_Input = null;
        }
        
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM

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
            
            
            m_Input.GetSubmodule<InputSystemSubmodule>().Enable();
            
            m_Input.GetSubmodule<InputSystemSubmodule>().AddListener<float>($"{mapName}/{actionName}", ctx => {
                isTriggered = true;
                triggeredResult = ctx.value;
            });

            // TODO: Need to implement InputSystem Input simulation
            m_Input.GetSubmodule<InputSystemSubmodule>().SimulateInputAction<float>($"{mapName}/{actionName}", -1.0f);

            Assert.IsTrue(isTriggered);
            Assert.AreEqual(-1.0f, triggeredResult);
            
            Object.DestroyImmediate(playerInput.gameObject);
        }
#endif
        
        [Test]
        public void SimulateInputManager_ShouldWorkCorrectly()
        {
            bool isTriggered = false;
            float triggeredResult = 1.0f;
            const string actionName = "Horizontal";
            
            m_Input.GetSubmodule<InputManagerSubmodule>().Enable();
            m_Input.GetSubmodule<InputManagerSubmodule>().AddListener<float>($"{actionName}", ctx =>
            {
                isTriggered = true;
                triggeredResult = ctx.value;
            });
            
            m_Input.GetSubmodule<InputManagerSubmodule>().SimulateInputAction<float>($"{actionName}", -1.0f);

            Assert.IsTrue(isTriggered);
            Assert.AreEqual(-1.0f, triggeredResult);
        }
    }
}

#endif