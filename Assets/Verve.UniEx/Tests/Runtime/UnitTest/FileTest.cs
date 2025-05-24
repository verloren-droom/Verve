namespace VerveUniEx.Tests
{
    using File;
    using System.IO;
    using Verve.Unit;
    using Verve.Serializable;
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
            string relativePath = "testFileUniEx.txt";
            string testData = "Hello, World!";
        
            m_FileUnit.WriteFile<JsonSerializableService, string>(relativePath, testData);
            bool result = m_FileUnit.TryReadFile<JsonSerializableService, string>(relativePath, out string data);
        
            Assert.IsTrue(result);
            Assert.AreEqual(testData, data);

            var del = m_FileUnit.DeleteFile(m_FileUnit.GetFullFilePath(relativePath));
            Assert.IsTrue(del);
        }
        
        [Test]
        public void WriteFile_ShouldWorkCorrectly()
        {
            string relativePath = "testFileUniEx.txt";
            string testData = "Hello, World!";
        
            bool result = m_FileUnit.WriteFile<JsonSerializableService, string>(relativePath, testData);
        
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(m_FileUnit.GetFullFilePath(relativePath)));
            
            var del = m_FileUnit.DeleteFile(m_FileUnit.GetFullFilePath(relativePath));
            Assert.IsTrue(del);
        }
        
        [Test]
        public void WriteFileWithOverwrite_ShouldWorkCorrectly()
        {
            string relativePath = "testFileUniEx.txt";
            string initialData = "Initial Data";
            string newData = "New Data";
        
            m_FileUnit.WriteFile<JsonSerializableService, string>(relativePath, initialData);
        
            bool result = m_FileUnit.WriteFile<JsonSerializableService, string>(relativePath, newData, true);
        
            Assert.IsTrue(result);
        
            m_FileUnit.TryReadFile<JsonSerializableService, string>(relativePath, out string data);
            Assert.AreEqual(newData, data);
            
            var del = m_FileUnit.DeleteFile(m_FileUnit.GetFullFilePath(relativePath));
            Assert.IsTrue(del);
        }
    }
}