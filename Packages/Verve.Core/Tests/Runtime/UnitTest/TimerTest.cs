namespace Verve.Tests
{
    using Unit;
    using Timer;
    using System;
    using NUnit.Framework;

    
    [TestFixture]
    public class TimerTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private TimerUnit m_TimerUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<TimerUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_TimerUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_TimerUnit = null;
        }
        
        [Test]
        public void AddTimer_ShouldWorkCorrectly()
        {
            bool timerCompleted = false;
            Action onComplete = () => timerCompleted = true;

            m_TimerUnit.AddTimer<SimpleTimerService>(1.0f, onComplete);
            ((ICustomUnit)m_TimerUnit).Tick(1.0f, 1.0f);

            Assert.IsTrue(timerCompleted);
        }

        [Test]
        public void RemoveTimer_ShouldWorkCorrectly()
        {
            bool timerCompleted = false;
            Action onComplete = () => timerCompleted = true;

            m_TimerUnit.AddTimer<SimpleTimerService>(1.0f, onComplete);
            m_TimerUnit.RemoveTimer<SimpleTimerService>(onComplete);
            
            ((ICustomUnit)m_TimerUnit).Tick(1.0f, 1.0f);

            Assert.IsFalse(timerCompleted);
        }

    }
}