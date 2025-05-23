namespace Verve.Tests
{
    using Unit;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class UnitSystemTest
    {
        [CustomUnit("TestUnitInterface")]
        public class TestUnitInterface : ICustomUnit
        {
            public string UnitName => "TestUnit";
            public int Priority => 0;
            public bool CanEverTick => false;
        
            public void Startup(UnitRules parent, params object[] args) { }
            public void Tick(float deltaTime, float unscaledTime) { }
            public void Shutdown() { }
            public void Dispose() { }
        }

        
        [CustomUnit("TestUnitBase")]
        public class TestUnitBase : UnitBase
        {
            public override string UnitName => "TestUnitBase";
            public override int Priority => 0;
            protected override void OnStartup(params object[] args) { }
        }


        /// <summary>
        /// 测试单元初始化
        /// </summary>
        [Test]
        public void InitializeUnit_ShouldWorkCorrectly()
        {
            var unitRules = new UnitRules();
        
            unitRules.AddDependency<TestUnitInterface>();
            unitRules.AddDependency("TestUnitBase");
            unitRules.Initialize();
        
            Assert.IsTrue(unitRules.TryGetDependency<TestUnitInterface>(out var testUnit1));
            Assert.IsNotNull(testUnit1);
            
            Assert.IsTrue(unitRules.TryGetDependency<TestUnitBase>(out var testUnit2));
            Assert.IsNotNull(testUnit2);
        }
        
        /// <summary>
        /// 测试单元未初始化异常
        /// </summary>
        [Test]
        public void UnitRulesNotInitializeException_ShouldBeThrown()
        {
            var unitRules = new UnitRules();
        
            unitRules.AddDependency<TestUnitInterface>();
        
            Assert.Throws<UnitRulesNotInitializeException>(() => unitRules.TryGetDependency<TestUnitInterface>(out var testUnit));
        }

        
        /// <summary>
        /// 测试单元未找到异常
        /// </summary>
        [Test]
        public void UnitNotFoundException_ShouldBeThrown()
        {
            var unitRules = new UnitRules();
            
            unitRules.Initialize();
            
            Assert.Throws<UnitNotFoundException>(() => unitRules.AddDependency("NonExistentUnit"));
        }
    }
}