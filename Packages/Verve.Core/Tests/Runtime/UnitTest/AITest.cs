namespace Verve.Tests
{
    using AI;
    using Unit;
    using System.Linq;
    using NUnit.Framework;
    using System.Collections.Generic;
    
    
    [TestFixture]
    public class AITest
    {
        private UnitRules m_UnitRules = new UnitRules();
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
            m_AIUnit = null;
        }
        
        [Test]
        public void BehaviorTree_ShouldExecuteNodesCorrectly()
        {
            var bb = new Blackboard();
            var tree = m_AIUnit.CreateBT<BehaviorTree>(bb: bb);
    
            bool action1Executed = false;

            tree.AddNode(new ActionNode { 
                Callback = _ => { 
                    action1Executed = true; 
                    return NodeStatus.Success;
                } 
            });

            ((IBehaviorTree)tree).Update(0.1f);
            Assert.IsTrue(action1Executed);
        }

        [Test]
        public void Blackboard_ShouldWorkCorrectly()
        {
            const int value1 = 1;
            const float value2 = -1.0f;
            
            var bb = new Blackboard();
            
            bb.SetValue("key1", value1);
            bb.SetValue("key2", value2);
            
            Assert.AreEqual(value1, bb.GetValue<int>("key1"));
            Assert.AreEqual(value2, bb.GetValue<float>("key2"));
        }
        
        [Test]
        public void ActionNode_ShouldExecuteAction()
        {
            bool executed = false;
            var actionNode = new ActionNode {
                Callback = _ => { executed = true; return NodeStatus.Success; }
            };
            
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = (actionNode as IBTNode).Run(ref ctx);
            
            Assert.IsTrue(executed);
            Assert.AreEqual(NodeStatus.Success, status);
        }
        
        [Test]
        public void SequenceNode_ShouldStopOnFailure()
        {
            var sequence = new SequenceNode {
                Children = new IBTNode[] {
                    new ActionNode { Callback = _ => NodeStatus.Success },
                    new ConditionNode { Condition = _ => false },
                    new ActionNode { Callback = _ => { Assert.Fail("Should not reach here");  return NodeStatus.Success; } }
                }
            };
            
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = (sequence as IBTNode).Run(ref ctx);
            
            Assert.AreEqual(NodeStatus.Failure, status);
        }

        [Test]
        public void SequenceNode_ShouldRememberRunningState()
        {
            var callCount = 0;
            var sequence = new SequenceNode {
                Children = new IBTNode[] {
                    new DelayNode { Duration = 0.5f },
                    new ActionNode { Callback = _ => {
                        callCount++;
                        return NodeStatus.Success;
                    }}
                }
            };

            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            (sequence as IBTNode).Run(ref ctx);
            Assert.AreEqual(0, callCount);
            
            (sequence as IBTNode).Run(ref ctx);
            Assert.AreEqual(0, callCount);

            ctx.DeltaTime = 0.5f;
            (sequence as IBTNode).Run(ref ctx);
            Assert.AreEqual(1, callCount);
        }
        
        [Test]
        public void ParallelNode_ShouldRequireAllSuccessWhenConfigured()
        {
            var parallel = new ParallelNode {
                Children = new IBTNode[] {
                    new ActionNode { Callback = _ => NodeStatus.Success },
                    new ActionNode { Callback = _ => NodeStatus.Running }
                },
                RequireAllSuccess = true
            };
        
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = (parallel as IBTNode).Run(ref ctx);
            
            Assert.AreEqual(NodeStatus.Running, status);
        }
        
        [Test]
        public void ParallelNode_ShouldRememberChildStates()
        {
            var parallel = new ParallelNode {
                Children = new IBTNode[] {
                    new DelayNode { Duration = 0.3f },
                    new DelayNode { Duration = 0.6f }
                }
            };
        
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = (parallel as IBTNode).Run(ref ctx);
            Assert.AreEqual(NodeStatus.Running, status);
            
            ctx.DeltaTime = 0.3f;
            status = (parallel as IBTNode).Run(ref ctx);
            Assert.AreEqual(NodeStatus.Running, status);
            
            ctx.DeltaTime = 0.3f;
            status = (parallel as IBTNode).Run(ref ctx);
            Assert.AreEqual(NodeStatus.Success, status);
        }
        
        [Test]
        public void ConditionNode_ShouldHandleNullCondition()
        {
            var node = new ConditionNode { Condition = null };
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = (node as IBTNode).Run(ref ctx);
            
            Assert.AreEqual(NodeStatus.Failure, status);
        }
        
        [Test]
        public void WaitNode_ShouldCompleteAfterDuration()
        {
            IBTNode node = new DelayNode { Duration = 0.5f };
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = node.Run(ref ctx);
            var wait = (DelayNode)node;
            Assert.AreEqual(NodeStatus.Running, status);
            Assert.AreEqual(0.1f, wait.ElapsedTime, 0.0001f);
        
            node = wait;

            ctx.DeltaTime = 0.4f;
            status = node.Run(ref ctx);
            wait = (DelayNode)node;
            Assert.AreEqual(NodeStatus.Success, status);
            Assert.AreEqual(0.5f, wait.ElapsedTime, 0.0001f);
        }
        
        [Test]
        public void WaitNode_ShouldResetProperly()
        {
            IBTNode node = new DelayNode { Duration = 0.3f };
            var bb = new Blackboard();
            var ctx = new NodeRunContext()
            {
                BB = bb,
                DeltaTime = 0.1f
            };
            var status = node.Run(ref ctx);

            var resetCtx = new NodeResetContext()
            {
                BB = bb,
            };
            (node as IResetableNode).Reset(ref resetCtx);

            ctx.DeltaTime = 0.2f;
            status = node.Run(ref ctx);
            Assert.AreEqual(NodeStatus.Running, status);
            
            ctx.DeltaTime = 0.1f;
            status = node.Run(ref ctx);
            Assert.AreEqual(NodeStatus.Success, status);
        }

        [Test]
        public void AllAITest_ShouldWorkCorrectly()
        {
            // 初始化黑板和日志记录
            var bb = new Blackboard();
            var executionLog = new List<string>();

            // 构建完整行为树
            var behaviorTree = m_AIUnit.CreateBT<BehaviorTree>(bb: bb);

            // 创建深度嵌套的节点结构
            IBTNode rootNode = new RepeaterNode()
            {
                Mode = RepeaterNode.RepeatMode.CountLimited,
                RepeatCount = 2,
                Child = new SequenceNode()
                {
                    Children = new IBTNode[]
                    {
                        new ParallelNode()
                        {
                            RequireAllSuccess = true,
                            Children = new IBTNode[]
                            {
                                new ConditionNode()
                                {
                                    Condition = b => b.GetValue<bool>("CanStart")
                                },
                                new DelayNode()
                                {
                                    Duration = 0.3f,
                                },
                                new ActionNode()
                                {
                                    Callback = _ =>
                                    {
                                        executionLog.Add($"Wait Finish!");
                                        return NodeStatus.Success;
                                    }
                                },
                            }
                        },
                        new RepeaterNode()
                        {
                            Mode = RepeaterNode.RepeatMode.CountLimited,
                            RepeatCount = 1,
                            Child = new SequenceNode()
                            {
                                Children = new IBTNode[]
                                {
                                    new ActionNode()
                                    {
                                        Callback = _ =>
                                        {
                                            bb.SetValue("Counter", bb.GetValue<int>("Counter") + 1);
                                            executionLog.Add("CounterAction");
                                            return NodeStatus.Success;
                                        }
                                    },
                                    new ConditionNode()
                                    {
                                        Condition = b => b.GetValue<int>("Counter") > 0
                                    }
                                }
                            }
                        },
                        new ActionNode()
                        {
                            Callback = _ =>
                            {
                                executionLog.Add("FinalAction");
                                return NodeStatus.Success;
                            }
                        }
                    }
                }
            };

            // 将节点添加到行为树
            behaviorTree.AddNode((RepeaterNode)rootNode);

            bb.SetValue("CanStart", false);
            bb.SetValue("Counter", 0);

            // 执行行为树（应被条件阻断）
            ((IBehaviorTree)behaviorTree).Update(0.1f);
            Assert.AreEqual(0, executionLog.Count);

            behaviorTree.ResetAllNodes();
            bb.SetValue("CanStart", true);
            
            for (int i = 0; i < 4; i++)
            {
                ((IBehaviorTree)behaviorTree).Update(0.1f);
            }

            // 验证日志顺序
            var expectedLogs = new[]
            {
                "Wait Finish!",
                "CounterAction",
                "FinalAction"
            };
            CollectionAssert.AreEqual(expectedLogs, executionLog.Take(expectedLogs.Length).ToArray());

            executionLog.Clear();

            // 执行完整循环（2次重复）
            for (int cycle = 0; cycle < 4; cycle++)
            {
                ((IBehaviorTree)behaviorTree).Update(0.1f);
            }

            // 验证最终状态
            Assert.AreEqual(1, executionLog.Count(x => x == "FinalAction"));
            Assert.AreEqual(1, bb.GetValue<int>("Counter"));
        }
    }
}