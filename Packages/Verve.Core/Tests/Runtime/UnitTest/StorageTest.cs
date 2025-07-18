using Verve.Platform;

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
        private StorageFeature m_Storage;

        
        [SetUp]
        public void SetUp()
        {
            m_Storage = new StorageFeature();
            var serializable = new SerializableFeature();
            var platform = new PlatformFeature();
            var file = new FileFeature();
            ((IGameFeature)serializable).Load(null);
            ((IGameFeature)platform).Load(null);
            ((IGameFeature)file).Load(null);
            ((IGameFeature)m_Storage).Load(new FeatureDependencies(new System.Collections.Generic.Dictionary<string, IGameFeature>()
            {
                { "Verve.Serializable", serializable },
                { "Verve.Platform", platform },
                { "Verve.File", file },
            }));
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
        public void WriteAndReadData_ShouldWorkCorrectly()
        {
            string fileName = "testFile";
            string key = "testKey";
            string value = "testValue";
        
            m_Storage.GetSubmodule<JsonStorageSubmodule>().Write<string>(fileName, key, value);
        
            bool result = m_Storage.GetSubmodule<JsonStorageSubmodule>().TryRead<string>(fileName, key, out string outValue);
        
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
        
            m_Storage.GetSubmodule<JsonStorageSubmodule>().Write<string>(fileName, key, value);
            m_Storage.GetSubmodule<JsonStorageSubmodule>().Delete(null, key);
            m_Storage.GetSubmodule<JsonStorageSubmodule>().DeleteAll(fileName);
        
            bool result = m_Storage.GetSubmodule<JsonStorageSubmodule>().TryRead<string>(fileName, key, out string outValue);
        
            Assert.IsFalse(result);
            Assert.IsNull(outValue);
        }
    }
}