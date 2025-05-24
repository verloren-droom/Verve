#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using Loader;
    using Verve.Unit;
    using UnityEngine;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class LoaderTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private LoaderUnit m_LoaderUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<LoaderUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_LoaderUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_LoaderUnit = null;
        }

        [Test]
        public void AddressablesAssetLoad_ShouldWorkCorrectly()
        {
            var testAssetPath = "Assets/Verve.UniEx/Tests/Art/test.txt";
            
            var obj = m_LoaderUnit.LoadAsset<AddressablesLoader, TextAsset>(testAssetPath);
            
            Assert.IsNotNull(obj);
        }
        
        [Test]
        public void ResourceAssetLoad_ShouldWorkCorrectly()
        {
            var testAssetPath = "Cube";
            
            var obj = m_LoaderUnit.LoadAsset<ResourcesLoader, GameObject>(testAssetPath);
            
            Assert.IsNotNull(obj);
        }
    }
}

#endif