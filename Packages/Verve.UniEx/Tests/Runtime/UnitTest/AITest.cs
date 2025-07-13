#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Tests
{
    using AI;
    using Verve;
    using Verve.AI;
    using Verve.Unit;
    using UnityEngine;
    using NUnit.Framework;
    using System.Collections;


    [TestFixture]
    public class AITest
    {
        private AIFeatureComponent m_AI;
        
        
        [SetUp]
        public void SetUp()
        {
            m_AI = new AIFeatureComponent();
            ((IGameFeature)m_AI).Load(null);
            ((IGameFeature)m_AI).Activate();
        }
        
        [TearDown]
        public void Teardown()
        {

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

#endif