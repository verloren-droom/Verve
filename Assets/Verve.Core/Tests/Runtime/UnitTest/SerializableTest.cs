namespace Verve.Tests
{
    using Unit;
    using Serializable;
    using NUnit.Framework;
    
    
    [TestFixture]
    public class SerializableTest
    {
        private UnitRules m_UnitRules = new UnitRules();
        private SerializableUnit m_SerializableUnit;

        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<SerializableUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_SerializableUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_SerializableUnit = null;
        }
        
        [Test]
        public void JsonSerialize_ShouldWorkCorrectly()
        {
            var testSerializable = new TestSerializable
            {
                Name = "Test",
                Age = 18,
                IsMarried = true,
                Height = 1.8f,
                Weight = 80.0,
                Gender = 'M',
            };
            
            var serialized = m_SerializableUnit.Serialize<JsonSerializableConverter>(testSerializable);

            Assert.AreEqual(serialized,
                "{\"Name\":\"Test\",\"Age\":18,\"IsMarried\":true,\"Height\":1.8,\"Weight\":80.0,\"Gender\":\"M\"}");
        }
        
        [Test]
        public void JsonDeserialize_ShouldWorkCorrectly()
        {
            var serialized = "{\"Name\":\"Test\",\"Age\":18,\"IsMarried\":true,\"Height\":1.8,\"Weight\":80.0,\"Gender\":\"M\"}";
            
            var testSerializable = m_SerializableUnit.Deserialize<JsonSerializableConverter, TestSerializable>(serialized);
            
            Assert.AreEqual(testSerializable.Name, "Test");
            Assert.AreEqual(testSerializable.Age, 18);
            Assert.AreEqual(testSerializable.IsMarried, true);
            Assert.AreEqual(testSerializable.Height, 1.8f);
            Assert.AreEqual(testSerializable.Weight, 80.0);
            Assert.AreEqual(testSerializable.Gender, 'M');
        }
        
        
        [System.Serializable]
        private class TestSerializable
        {
            public string Name;
            public int Age;
            public bool IsMarried;
            public float Height;
            public double Weight;
            public char Gender;
        }
    }
}