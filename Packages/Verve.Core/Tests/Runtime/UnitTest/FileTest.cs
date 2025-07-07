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
        private FileFeature m_File;


        [SetUp]
        public void SetUp()
        {
            m_File = new FileFeature();
            ((IGameFeature)m_File).Load();
            ((IGameFeature)m_File).Activate();
        }

        [TearDown]
        public void Teardown()
        {
            m_File = null;
        }

        [Test]
        public void TryReadFile_ShouldWorkCorrectly()
        {
            string relativePath = "testFile.txt";
            string testData = "Hello, World!";
        
            m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, testData);
            bool result = m_File.TryReadFile<JsonSerializableSubmodule, string>(relativePath, out string data);
        
            Assert.IsTrue(result);
            Assert.AreEqual(testData, data);
        }
        
        [Test]
        public void WriteFile_ShouldWorkCorrectly()
        {
            string relativePath = "testFile.txt";
            string testData = "Hello, World!";
        
            bool result = m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, testData);
        
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(m_File.GetFullFilePath(relativePath)));
        }
        
        [Test]
        public void WriteFileWithOverwrite_ShouldWorkCorrectly()
        {
            string relativePath = "testFile.txt";
            string initialData = "Initial Data";
            string newData = "New Data";
        
            m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, initialData);
        
            bool result = m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, newData, true);
        
            Assert.IsTrue(result);
        
            m_File.TryReadFile<JsonSerializableSubmodule, string>(relativePath, out string data);
            Assert.AreEqual(newData, data);
        }
    }
}