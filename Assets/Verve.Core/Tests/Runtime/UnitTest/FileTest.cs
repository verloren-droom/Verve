namespace Verve.Tests
{
    using File;
    using Unit;
    using System.IO;
    using Serializable;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class FileTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private FileUnit m_FileUnit;


        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<SerializableUnit>();
            m_UnitRules.AddDependency<FileUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_FileUnit);
        }

        [TearDown]
        public void Teardown()
        {
            m_FileUnit = null;
        }

        [Test]
        public void TryReadFile_ShouldWorkCorrectly()
        {
            string relativePath = "testFile.txt";
            string testData = "Hello, World!";
        
            m_FileUnit.WriteFile<JsonSerializableConverter, string>(relativePath, testData);
            bool result = m_FileUnit.TryReadFile<JsonSerializableConverter, string>(relativePath, out string data);
        
            Assert.IsTrue(result);
            Assert.AreEqual(testData, data);
        }
        
        [Test]
        public void WriteFile_ShouldWorkCorrectly()
        {
            string relativePath = "testFile.txt";
            string testData = "Hello, World!";
        
            bool result = m_FileUnit.WriteFile<JsonSerializableConverter, string>(relativePath, testData);
        
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(FileDefine.GetPersistentFilePath(relativePath)));
        }
        
        [Test]
        public void WriteFileWithOverwrite_ShouldWorkCorrectly()
        {
            string relativePath = "testFile.txt";
            string initialData = "Initial Data";
            string newData = "New Data";
        
            m_FileUnit.WriteFile<JsonSerializableConverter, string>(relativePath, initialData);
        
            bool result = m_FileUnit.WriteFile<JsonSerializableConverter, string>(relativePath, newData, true);
        
            Assert.IsTrue(result);
        
            m_FileUnit.TryReadFile<JsonSerializableConverter, string>(relativePath, out string data);
            Assert.AreEqual(newData, data);
        }
    }
}