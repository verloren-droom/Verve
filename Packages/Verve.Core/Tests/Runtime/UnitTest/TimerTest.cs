namespace Verve.Tests
{
    using Unit;
    using Timer;
    using System;
    using NUnit.Framework;

    
    [TestFixture]
    public class TimerTest
    {
        private TimerFeature m_Timer;

        
        [SetUp]
        public void SetUp()
        {
            m_Timer = new TimerFeature();
            ((IGameFeature)m_Timer).Load(null);
            ((IGameFeature)m_Timer).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_Timer = null;
        }
        
        [Test]
        public void AddTimer_ShouldWorkCorrectly()
        {
            bool timerCompleted = false;
            Action onComplete = () => timerCompleted = true;

            m_Timer.AddTimer<SimpleTimerSubmodule>(1.0f, onComplete);
            m_Timer.OnUpdate(1.0f);
            

            Assert.IsTrue(timerCompleted);
        }

        [Test]
        public void RemoveTimer_ShouldWorkCorrectly()
        {
            bool timerCompleted = false;
            Action onComplete = () => timerCompleted = true;

            m_Timer.AddTimer<SimpleTimerSubmodule>(1.0f, onComplete);
            m_Timer.RemoveTimer<SimpleTimerSubmodule>(onComplete);
            
            m_Timer.OnUpdate(1.0f);

            Assert.IsFalse(timerCompleted);
        }

    }
}