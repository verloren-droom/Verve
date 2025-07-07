#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using Verve;
    using System;
    using Loader;
    using System.IO;
    using Verve.Unit;
    using UnityEngine;
    using NUnit.Framework;


    [TestFixture]
    public class LoaderTest
    {
        private LoaderFeature m_LoaderUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_LoaderUnit = new LoaderFeature();
            ((IGameFeature)m_LoaderUnit).Load();
            ((IGameFeature)m_LoaderUnit).Activate();
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