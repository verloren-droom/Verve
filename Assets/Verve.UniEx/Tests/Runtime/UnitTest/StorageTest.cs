namespace VerveUniEx.Tests
{
    using File;
    using Storage;
    using Verve.Unit;
    using NUnit.Framework;
    using Verve.Serializable;
    
    
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
        public void WriteAndReadDataByJson_ShouldWorkCorrectly()
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
        public void DeleteDataByJson_ShouldWorkCorrectly()
        {
            string key = "testKey";
            string value = "testValue";
        
            m_StorageUnit.Write<JsonStorage, string>(key, value);
            m_StorageUnit.Delete<JsonStorage>(key);
        
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(key, out string outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
        }
        
        /// <summary>
        /// 测试同步写入和读取多组不同数据
        /// </summary>
        [Test]
        public void WriteAndReadMultiDataByJson_ShouldWorkCorrectly()
        {
            string key1 = "testKey";
            string value1 = "testValue";
            string key2 = "testKey2";
            bool value2 = false;
            string key3 = "testKey3";
            int value3 = 123;
            string key4 = "testKey4";
            float value4 = 1.23f;
            string key5 = "testKey5";
            CustomTestData value5 = new CustomTestData()
            {
                Name = "testName",
                Age = 18,
                IsMarried = true,
                Height = 1.23f
            };
        
            m_StorageUnit.Write<JsonStorage, string>(key1, value1);
            m_StorageUnit.Write<JsonStorage, bool>(key2, value2);
            m_StorageUnit.Write<JsonStorage, int>(key3, value3);
            m_StorageUnit.Write<JsonStorage, float>(key4, value4);
            m_StorageUnit.Write<JsonStorage, CustomTestData>(key5, value5);
        
            bool result1 = m_StorageUnit.TryRead<JsonStorage, string>(key1, out string outValue1);
            bool result2 = m_StorageUnit.TryRead<JsonStorage, bool>(key2, out bool outValue2);
            bool result3 = m_StorageUnit.TryRead<JsonStorage, int>(key3, out int outValue3);
            bool result4 = m_StorageUnit.TryRead<JsonStorage, float>(key4, out float outValue4);
            bool result5 = m_StorageUnit.TryRead<JsonStorage, CustomTestData>(key5, out CustomTestData outValue5);

            Assert.IsTrue(result1);
            Assert.AreEqual(value1, outValue1);
            
            Assert.IsTrue(result2);
            Assert.AreEqual(value2, outValue2);
            
            Assert.IsTrue(result3);
            Assert.AreEqual(value3, outValue3);
            
            Assert.IsTrue(result4);
            Assert.AreEqual(value4, outValue4);
            
            Assert.IsTrue(result5);
            Assert.AreEqual(value5.Name, outValue5.Name);
        }
        
        [Test]
        public void WriteAndReadDataAtCustomFile_ShouldWorkCorrectly()
        {
            string key = "testKey";
            string value = "testValue";
            string fileName = "customFile";
            
            m_StorageUnit.Write<JsonStorage, string>(fileName, key, value);
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(fileName, key, out string outValue);
            
            Assert.IsTrue(result);
            Assert.AreEqual(value, outValue);
        }
        
        /// <summary>
        /// 测试删除数据
        /// </summary>
        [Test]
        public void DeleteDataByJsonAtCustomFile_ShouldWorkCorrectly()
        {
            string key = "testKey";
            string value = "testValue";
            string fileName = "customFile";

            m_StorageUnit.Write<JsonStorage, string>(fileName, key, value);
            m_StorageUnit.Delete<JsonStorage>(fileName, key);
        
            bool result = m_StorageUnit.TryRead<JsonStorage, string>(fileName, key, out string outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
        }
        
        [System.Serializable]
        private struct CustomTestData
        {
            public string Name;
            public int Age;
            public bool IsMarried;
            public float Height;
        }
    }
}