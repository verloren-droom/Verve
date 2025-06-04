namespace Verve.Tests
{
    using Unit;
    using File;
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
            m_UnitRules.AddDependency<FileUnit>();
            m_UnitRules.AddDependency<StorageUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_StorageUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_StorageUnit = null;
        }

        /// <summary>
        /// 测试同步写入和读取数据
        /// </summary>
        [Test]
        public void WriteAndReadData_ShouldWorkCorrectly()
        {
            string fileName = "testFile";
            string key = "testKey";
            string value = "testValue";
        
            m_StorageUnit.Write<JsonStorage, string>(fileName, key, value);
        
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(fileName, key, out string outValue);
        
            Assert.IsTrue(result);
            Assert.AreEqual(value, outValue);
        }
        
        /// <summary>
        /// 测试删除数据
        /// </summary>
        [Test]
        public void DeleteData_ShouldWorkCorrectly()
        {
            string fileName = "testFile";
            string key = "testKey";
            string value = "testValue";
        
            m_StorageUnit.Write<JsonStorage, string>(fileName, key, value);
            m_StorageUnit.Delete<JsonStorage>(key);
            m_StorageUnit.DeleteAll<JsonStorage>(fileName);
        
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(fileName, key, out string outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
        }
    }
}