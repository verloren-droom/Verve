namespace VerveUniEx.Tests
{
    using AI;
    using Verve.AI;
    using Verve.Unit;
    using UnityEngine;
    using NUnit.Framework;
    using System.Collections;
    
    
    [TestFixture]
    public class AITest
    {
        private UnitRules m_UnitRules;
        private AIUnit m_AIUnit;
        
        
        [SetUp]
        public void SetUp()
        {
            m_UnitRules = new UnitRules();
            m_UnitRules.AddDependency<AIUnit>();
            m_UnitRules.Initialize();
            m_UnitRules.TryGetDependency(out m_AIUnit);
        }
        
        [TearDown]
        public void Teardown()
        {
            m_UnitRules.DeInitialize();
            m_UnitRules.Dispose();
            m_AIUnit.Dispose();
        }

        [Test]
        public void DistanceConditionNode_ShouldReturnCorrectStatus()
        {
            var bb = new Blackboard();
            
            var node = new DistanceConditionNode
            {
                OwnerPoint = Vector3.zero,
                TargetPoint = Vector3.forward * 3,
                CheckDistance = 5f,
                CompareMode = DistanceConditionNode.Comparison.LessThanOrEqual
            };
        
            var status = (node as IBTNode).Run(ref bb, 0);
            Assert.AreEqual(NodeStatus.Success, status);
        
            node.TargetPoint = Vector3.forward * 6;
            status = (node as IBTNode).Run(ref bb, 0);
            Assert.AreEqual(NodeStatus.Failure, status);
        }
    }
}