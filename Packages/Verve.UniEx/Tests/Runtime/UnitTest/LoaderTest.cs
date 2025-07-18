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
        private LoaderFeature m_Loader;

        
        [SetUp]
        public void SetUp()
        {
            m_Loader = new LoaderFeature();
            ((IGameFeature)m_Loader).Load(null);
            ((IGameFeature)m_Loader).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {
            m_Loader = null;
        }

        [Test]
        public void AddressablesAssetLoad_ShouldWorkCorrectly()
        {
            var testAssetPath = "Assets/Verve.UniEx/Tests/Art/test.txt";

            var obj = m_Loader.GetSubmodule<AddressablesLoader>().LoadAsset<TextAsset>(testAssetPath);
            
            Assert.IsNotNull(obj);
        }
        
        [Test]
        public void ResourceAssetLoad_ShouldWorkCorrectly()
        {
            var testAssetPath = "Cube";
            
            var obj = m_Loader.GetSubmodule<ResourcesLoader>().LoadAsset<GameObject>(testAssetPath);
            
            Assert.IsNotNull(obj);
        }
    }
}

#endif