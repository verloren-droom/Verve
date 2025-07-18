namespace Verve.Tests
{
    using Event;
    using System;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class EventTest
    {
        private EventBusFeature m_Event;

        
        [SetUp]
        public void SetUp()
        {
            m_Event = new EventBusFeature();
            ((IGameFeature)m_Event).Load(null);
            ((IGameFeature)m_Event).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_Event = null;
        }
        
        /// <summary>
        /// 测试枚举事件的添加和触发
        /// </summary>
        [Test]
        public void AddAndInvokeEnumEvent_ShouldInvokeListener()
        {
            bool eventTriggered = false;
            var eventType = TestEnum.Event1;
        
            m_Event.GetSubmodule<EnumEventBusSubmodule>().AddListener(eventType, (TestEventArgs args) => eventTriggered = true);
            m_Event.GetSubmodule<EnumEventBusSubmodule>().Invoke(eventType, new TestEventArgs());
            
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
            
            m_Event.GetSubmodule<EnumEventBusSubmodule>().AddListener(eventType, handler);
            m_Event.GetSubmodule<EnumEventBusSubmodule>().RemoveListener(eventType, handler);
            m_Event.GetSubmodule<EnumEventBusSubmodule>().Invoke(eventType, new TestEventArgs());
            
            Assert.IsFalse(eventTriggered);
        }

        /// <summary>
        /// 测试字符串事件的添加和触发
        /// </summary>
        [Test]
        public void AddAndInvokeStringEvent_ShouldInvokeListener()
        {
            bool eventTriggered = false;
            
            m_Event.GetSubmodule<StringEventBusSubmodule>().AddListener<object>("testEvent", (_) => eventTriggered = true);
            m_Event.GetSubmodule<StringEventBusSubmodule>().Invoke<object>("testEvent", null);
            
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
            
            m_Event.GetSubmodule<StringEventBusSubmodule>().AddListener("testEvent", handler);
            m_Event.GetSubmodule<StringEventBusSubmodule>().RemoveListener("testEvent", handler);
            m_Event.GetSubmodule<StringEventBusSubmodule>().Invoke<object>("testEvent", null);
            
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
        
            m_Event.GetSubmodule<EnumEventBusSubmodule>().AddListener(eventType, (TestEventArgs args) => enumEventTriggered = true);
            m_Event.GetSubmodule<StringEventBusSubmodule>().AddListener<object>("testEvent", (obj) => stringEventTriggered = true);
            m_Event.GetSubmodule<StringEventBusSubmodule>().RemoveAllListener();
            m_Event.GetSubmodule<EnumEventBusSubmodule>().RemoveAllListener();
            m_Event.GetSubmodule<EnumEventBusSubmodule>().Invoke(eventType, new TestEventArgs());
            m_Event.GetSubmodule<StringEventBusSubmodule>().Invoke<object>("testEvent", null);
            
            Assert.IsFalse(enumEventTriggered);
            Assert.IsFalse(stringEventTriggered);
        }


        private enum TestEnum
        {
            Event1,
            Event2
        }

        private class TestEventArgs
        {
            
        }
    }
}