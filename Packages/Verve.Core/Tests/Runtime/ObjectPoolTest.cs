namespace Verve.Tests
{
    using NUnit.Framework;
    

    [TestFixture]
    public class ObjectPoolTest
    {
        [Test]
        public void ObjectGetRelease()
        {
            const string DEFAULT_TIP = "default state";
            const string USE_TIP = "use state";
            const string UNUSE_TIP = "unuse state";
            const string DESTROY_TIP = "destroy state";
            
            var pool = new ObjectPool<PoolTest>(() => new PoolTest(DEFAULT_TIP, USE_TIP, UNUSE_TIP, DESTROY_TIP), e => e.Use(), e => e.Unuse(), e => e.Destroy());
            var obj = pool.Get();

            Assert.AreEqual(USE_TIP, obj.State);
            pool.Release(obj);
            Assert.AreEqual(UNUSE_TIP, obj.State);
            pool.Clear(true);
            Assert.AreEqual(DESTROY_TIP, obj.State);
        }
        
        
        private class PoolTest
        {
            public string State { get; private set; }

            private readonly string m_DefaultState, m_UseState, m_UnuseState, m_DestroyState;

            public PoolTest(string defaultState, string useState, string unuseState, string destroyState)
            {
                m_DefaultState = defaultState;
                m_UseState = useState;
                m_UnuseState = unuseState;
                m_DestroyState = destroyState;
                State = m_DefaultState;
            }

            public void Use()
            {
                State = m_UseState;
            }

            public void Unuse()
            {
                State = m_UnuseState;
            }

            public void Destroy()
            {
                State = m_DestroyState;
            }
        }
    }
}