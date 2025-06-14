namespace VerveUniEx.Tests
{
    using AI;
    using Verve.AI;
    using Verve.Unit;
    using UnityEngine;
    using NUnit.Framework;
    using System.Collections;
    using AIUnit = VerveUniEx.AI.AIUnit;
    
    
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
            
            var node = new VectorDistanceConditionBTNode
            {
                data = new VectorDistanceConditionBTNodeData()
                {
                    ownerPoint = Vector3.zero,
                    targetPoint = Vector3.forward * 3,
                    checkDistance = 5f,
                    compareMode = VectorDistanceConditionBTNodeData.Comparison.LessThanOrEqual
                }
            };
            
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0f
            };
            var status = (node as IBTNode).Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Succeeded, status);
        
            node.data.targetPoint = Vector3.forward * 6;
            status = (node as IBTNode).Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Failed, status);
        }
    }
}