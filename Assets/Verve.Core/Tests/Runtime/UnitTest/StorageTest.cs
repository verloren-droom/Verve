namespace Verve.Tests
{
    using Unit;
    using Storage;
    using Serializable;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class StorageTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private StorageUnit m_StorageUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<SerializableUnit>();
            m_UnitRules.AddDependency<StorageUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_StorageUnit);
        }

        /// <summary>
        /// 测试同步写入和读取数据
        /// </summary>
        [Test]
        public void WriteAndReadData_ShouldWorkCorrectly()
        {
            string key = "testKey";
            string value = "testValue";
        
            m_StorageUnit.Write<JsonStorage, string>(key, value);
        
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(key, out string outValue);
        
            Assert.IsTrue(result);
            Assert.AreEqual(value, outValue);
        }
        
        /// <summary>
        /// 测试删除数据
        /// </summary>
        [Test]
        public void DeleteData_ShouldWorkCorrectly()
        {
            string key = "testKey";
            string value = "testValue";
        
            m_StorageUnit.Write<JsonStorage, string>(key, value);
            m_StorageUnit.Delete<JsonStorage>(key);
        
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(key, out string outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
        }
    }
}