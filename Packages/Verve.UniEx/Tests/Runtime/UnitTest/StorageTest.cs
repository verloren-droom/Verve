#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using File;
    using Verve;
    using Storage;
    using Verve.Unit;
    using Verve.Storage;
    using NUnit.Framework;
    using Verve.Serializable;
    using StorageFeature = VerveUniEx.Storage.StorageFeature;
    
    
    [TestFixture]
    public class StorageTest
    {
        private StorageFeature m_Storage;

        
        [SetUp]
        public void SetUp()
        {
            m_Storage = new StorageFeature();
            ((IGameFeature)m_Storage).Load();
            ((IGameFeature)m_Storage).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_Storage = null;
        }

        /// <summary>
        /// 测试同步写入和读取数据
        /// </summary>
        [Test]
        public void WriteAndReadDataByJson_ShouldWorkCorrectly()
        {
            string key = "testKey";
            string value = "testValue";
        
            m_Storage.Write<JsonStorageSubmodule, string>(key, value);
        
            bool result = m_Storage.TryRead<JsonStorageSubmodule, string>(key, out var outValue);
        
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
        
            m_Storage.Write<JsonStorageSubmodule, string>(key, value);
            m_Storage.Delete<JsonStorageSubmodule>(key);
            m_Storage.DeleteAll<JsonStorageSubmodule>();
        
            bool result = m_Storage.TryRead<JsonStorageSubmodule, string>(key, out var outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
            Assert.AreNotEqual(value, outValue);
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
        
            m_Storage.Write<JsonStorageSubmodule, string>(key1, value1);
            m_Storage.Write<JsonStorageSubmodule, bool>(key2, value2);
            m_Storage.Write<JsonStorageSubmodule, int>(key3, value3);
            m_Storage.Write<JsonStorageSubmodule, float>(key4, value4);
            m_Storage.Write<JsonStorageSubmodule, CustomTestData>(key5, value5);
        
            bool result1 = m_Storage.TryRead<JsonStorageSubmodule, string>(key1, out var outValue1);
            bool result2 = m_Storage.TryRead<JsonStorageSubmodule, bool>(key2, out var outValue2);
            bool result3 = m_Storage.TryRead<JsonStorageSubmodule, int>(key3, out var outValue3);
            bool result4 = m_Storage.TryRead<JsonStorageSubmodule, float>(key4, out var outValue4);
            bool result5 = m_Storage.TryRead<JsonStorageSubmodule, CustomTestData>(key5, out var outValue5);

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
            string fileName = "customFile.txt";
            
            m_Storage.Write<JsonStorageSubmodule, string>(fileName, key, value);
            bool result = m_Storage.TryRead<JsonStorageSubmodule, string>(fileName, key, out var outValue);

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
            string fileName = "customFile.txt";

            m_Storage.Write<JsonStorageSubmodule, string>(fileName, key, value);
            m_Storage.Delete<JsonStorageSubmodule>(fileName, key);
            m_Storage.DeleteAll<JsonStorageSubmodule>(fileName);
        
            bool result = m_Storage.TryRead<JsonStorageSubmodule, string>(fileName, key, out var outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
            Assert.AreNotEqual(value, outValue);
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

#endif