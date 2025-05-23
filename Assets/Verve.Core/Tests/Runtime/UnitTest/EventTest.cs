namespace Verve.Tests
{
    using Unit;
    using Event;
    using System;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class EventTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private EventUnit m_EventUnit;

        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<EventUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_EventUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_EventUnit = null;
        }
        
        /// <summary>
        /// 测试枚举事件的添加和触发
        /// </summary>
        [Test]
        public void AddAndInvokeEnumEvent_ShouldInvokeListener()
        {
            bool eventTriggered = false;
            var eventType = TestEnum.Event1;
        
            m_EventUnit.AddListener(eventType, (TestEventArgs args) => eventTriggered = true);
            m_EventUnit.Invoke(eventType, new TestEventArgs());
            
            Assert.IsTrue(eventTriggered);
        }
        
        /// <summary>
        /// 测试枚举事件的移除
        /// </summary>
        [Test]
        public void RemoveEnumEvent_ShouldNotInvokeListener()
        {
            bool eventTriggered = false;
            var eventType = TestEnum.Event1;
            Action<TestEventArgs> handler = (args) => eventTriggered = true;
            
            m_EventUnit.AddListener(eventType, handler);
            m_EventUnit.RemoveListener(eventType, handler);
            m_EventUnit.Invoke(eventType, new TestEventArgs());
            
            Assert.IsFalse(eventTriggered);
        }
        
        /// <summary>
        /// 测试枚举事件的覆盖绑定
        /// </summary>
        [Test]
        public void BindEnumEvent_ShouldOverrideListener()
        {
            bool eventTriggered1 = false;
            bool eventTriggered2 = false;
            var eventType = TestEnum.Event1;

            m_EventUnit.BindListener(eventType, (TestEventArgs args) => eventTriggered1 = true, overwrite: true);
            m_EventUnit.BindListener(eventType, (TestEventArgs args) => eventTriggered2 = true, overwrite: true);
            m_EventUnit.Invoke(eventType, new TestEventArgs());
            
            Assert.IsFalse(eventTriggered1);
            Assert.IsTrue(eventTriggered2);
        }

        /// <summary>
        /// 测试枚举事件的不覆盖绑定
        /// </summary>
        [Test]
        public void BindEnumEvent_ShouldNotOverrideListener()
        {
            bool eventTriggered1 = false;
            bool eventTriggered2 = false;
            var eventType = TestEnum.Event2;

            m_EventUnit.BindListener(eventType, (TestEventArgs args) => eventTriggered1 = true, overwrite: false);
            m_EventUnit.BindListener(eventType, (TestEventArgs args) => eventTriggered2 = true, overwrite: false);
            m_EventUnit.Invoke(eventType, new TestEventArgs());
            
            Assert.IsTrue(eventTriggered1);
            Assert.IsFalse(eventTriggered2);
        }
        
        /// <summary>
        /// 测试字符串事件的添加和触发
        /// </summary>
        [Test]
        public void AddAndInvokeStringEvent_ShouldInvokeListener()
        {
            bool eventTriggered = false;
            
            m_EventUnit.AddListener("testEvent", (obj) => eventTriggered = true);
            m_EventUnit.Invoke("testEvent", null);
            
            Assert.IsTrue(eventTriggered);
        }

        /// <summary>
        /// 测试字符串事件的移除
        /// </summary>
        [Test]
        public void RemoveStringEvent_ShouldNotInvokeListener()
        {
            bool eventTriggered = false;
            Action<object> handler = (obj) => eventTriggered = true;
            
            m_EventUnit.AddListener("testEvent", handler);
            m_EventUnit.RemoveListener("testEvent", handler);
            m_EventUnit.Invoke("testEvent", null);
            
            Assert.IsFalse(eventTriggered);
        }
        
        /// <summary>
        /// 测试 RemoveAllListener 方法
        /// </summary>
        [Test]
        public void RemoveAllListener_ShouldRemoveAllListeners()
        {
            bool enumEventTriggered = false;
            bool stringEventTriggered = false;
            var eventType = TestEnum.Event1;
        
            m_EventUnit.AddListener(eventType, (TestEventArgs args) => enumEventTriggered = true);
            m_EventUnit.AddListener("testEvent", (obj) => stringEventTriggered = true);
            m_EventUnit.RemoveAllListener();
            m_EventUnit.Invoke(eventType, new TestEventArgs());
            m_EventUnit.Invoke("testEvent", null);
            
            Assert.IsFalse(enumEventTriggered);
            Assert.IsFalse(stringEventTriggered);
        }


        private enum TestEnum
        {
            Event1,
            Event2
        }

        private class TestEventArgs : EventArgsBase
        {
            
        }
    }
}