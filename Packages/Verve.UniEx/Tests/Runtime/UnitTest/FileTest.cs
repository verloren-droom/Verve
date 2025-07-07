#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using File;
    using Verve;
    using System.IO;
    using Verve.Unit;
    using NUnit.Framework;
    using Verve.Serializable;
    
    
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
            string relativePath = "testReadFileUniEx.txt";
            string testData = "Hello, World!";
        
            m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, testData);
            bool result = m_File.TryReadFile<JsonSerializableSubmodule, string>(relativePath, out string data);
        
            Assert.IsTrue(result);
            Assert.AreEqual(testData, data);

            var del = m_File.DeleteFile(m_File.GetFullFilePath(relativePath));
            Assert.IsTrue(del);
        }
        
        [Test]
        public void WriteFile_ShouldWorkCorrectly()
        {
            string relativePath = "testFileUniEx.txt";
            string testData = "Hello, World!";
        
            bool result = m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, testData);
        
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(m_File.GetFullFilePath(relativePath)));
            
            var del = m_File.DeleteFile(m_File.GetFullFilePath(relativePath));
            Assert.IsTrue(del);
        }
        
        [Test]
        public void WriteFileWithOverwrite_ShouldWorkCorrectly()
        {
            string relativePath = "testWriteFileUniEx.txt";
            string initialData = "Initial Data";
            string newData = "New Data";

            m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, initialData);
        
            bool result = m_File.WriteFile<JsonSerializableSubmodule, string>(relativePath, newData, true);
        
            Assert.IsTrue(result);
        
            m_File.TryReadFile<JsonSerializableSubmodule, string>(relativePath, out string data);
            Assert.AreEqual(newData, data);
            
            var del = m_File.DeleteFile(m_File.GetFullFilePath(relativePath));
            Assert.IsTrue(del);
        }
    }
}

#endif