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
            var tree = m_AIUnit.CreateBehaviorTree<BehaviorTree>(bb: bb);
    
            bool action1Executed = false;

            tree.AddNode(new ActionBTNode { 
                data = new ActionBTNodeData()
                {
                    callback = _ => { 
                        action1Executed = true; 
                        return BTNodeResult.Succeeded;
                    } 
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
            var actionNode = new ActionBTNode {
                data = new ActionBTNodeData()
                {
                    callback = _ => { executed = true; return BTNodeResult.Succeeded; }
                }
            };
            
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = (actionNode as IBTNode).Run(ref ctx);
            
            Assert.IsTrue(executed);
            Assert.AreEqual(BTNodeResult.Succeeded, status);
        }
        
        [Test]
        public void SequenceNode_ShouldStopOnFailure()
        {
            var sequence = new SequenceBTNode {
                data = new SequenceBTNodeData()
                {
                    children = new IBTNode[] {
                        new ActionBTNode
                        {
                            data = new ActionBTNodeData()
                            {
                                callback = _ => BTNodeResult.Succeeded
                            }
                        },
                        new ConditionBTNode
                        {
                            data = new ConditionBTNodeData()
                            {
                                condition = _ => false
                            }
                        },
                        new ActionBTNode
                        {
                            data = new ActionBTNodeData()
                            {
                                callback = _ => { Assert.Fail("Should not reach here");  return BTNodeResult.Succeeded;
                            }
                        }}
                    }
                }
            };
            
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = (sequence as IBTNode).Run(ref ctx);
            
            Assert.AreEqual(BTNodeResult.Failed, status);
        }

        [Test]
        public void SequenceNode_ShouldRememberRunningState()
        {
            var callCount = 0;
            var sequence = new SequenceBTNode {
                data = new SequenceBTNodeData()
                {
                    children = new IBTNode[] {
                        new DelayBTNode
                        {
                            data = new DelayBTNodeData()
                            {
                                duration = 0.5f
                            }
                        },
                        new ActionBTNode
                        {
                            data = new ActionBTNodeData()
                            {
                                callback = _ => { callCount++; return BTNodeResult.Succeeded; }
                            }
                        }
                    }
                }
            };

            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            (sequence as IBTNode).Run(ref ctx);
            Assert.AreEqual(0, callCount);
            
            (sequence as IBTNode).Run(ref ctx);
            Assert.AreEqual(0, callCount);

            ctx.deltaTime = 0.5f;
            (sequence as IBTNode).Run(ref ctx);
            Assert.AreEqual(1, callCount);
        }
        
        [Test]
        public void ParallelNode_ShouldRequireAllSuccessWhenConfigured()
        {
            var parallel = new ParallelBTNode {
                data = new ParallelBTNodeData()
                {
                    children = new IBTNode[] {
                        new ActionBTNode
                        {
                            data = new ActionBTNodeData()
                            {
                                callback = _ => BTNodeResult.Succeeded
                            }
                        },
                        new ActionBTNode
                        {
                            data = new ActionBTNodeData()
                            {
                                callback = _ => BTNodeResult.Running
                            }
                        }
                    },
                    requireAllSuccess = true
                }
            };
        
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = (parallel as IBTNode).Run(ref ctx);
            
            Assert.AreEqual(BTNodeResult.Running, status);
        }
        
        [Test]
        public void ParallelNode_ShouldRememberChildStates()
        {
            var parallel = new ParallelBTNode {
                data = new ParallelBTNodeData()
                {
                    children = new IBTNode[] {
                        new DelayBTNode
                        {
                            data = new DelayBTNodeData()
                            {
                                duration = 0.3f
                            }
                        },
                        new DelayBTNode
                        {
                            data = new DelayBTNodeData()
                            {
                                duration = 0.6f
                            }
                        }
                    }
                }
            };
        
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = (parallel as IBTNode).Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Running, status);
            
            ctx.deltaTime = 0.3f;
            status = (parallel as IBTNode).Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Running, status);
            
            ctx.deltaTime = 0.3f;
            status = (parallel as IBTNode).Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Succeeded, status);
        }
        
        [Test]
        public void ConditionNode_ShouldHandleNullCondition()
        {
            var node = new ConditionBTNode
            {
                data = new ConditionBTNodeData()
                {
                    condition = null
                }
            };
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = (node as IBTNode).Run(ref ctx);
            
            Assert.AreEqual(BTNodeResult.Failed, status);
        }
        
        [Test]
        public void WaitNode_ShouldCompleteAfterDuration()
        {
            IBTNode node = new DelayBTNode
            {
                data = new DelayBTNodeData()
                {
                    duration = 0.5f
                }
            };
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = node.Run(ref ctx);
            var wait = (DelayBTNode)node;
            Assert.AreEqual(BTNodeResult.Running, status);
            Assert.AreEqual(0.1f, wait.ElapsedTime, 0.0001f);
        
            node = wait;

            ctx.deltaTime = 0.4f;
            status = node.Run(ref ctx);
            wait = (DelayBTNode)node;
            Assert.AreEqual(BTNodeResult.Succeeded, status);
            Assert.AreEqual(0.5f, wait.ElapsedTime, 0.0001f);
        }
        
        [Test]
        public void WaitNode_ShouldResetProperly()
        {
            IBTNode node = new DelayBTNode
            {
                data = new DelayBTNodeData()
                {
                    duration = 0.3f
                }
            };
            var bb = new Blackboard();
            var ctx = new BTNodeRunContext()
            {
                bb = bb,
                deltaTime = 0.1f
            };
            var status = node.Run(ref ctx);

            var resetCtx = new BTNodeResetContext()
            {
                bb = bb,
            };
            (node as IBTNodeResettable).Reset(ref resetCtx);

            ctx.deltaTime = 0.2f;
            status = node.Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Running, status);
            
            ctx.deltaTime = 0.1f;
            status = node.Run(ref ctx);
            Assert.AreEqual(BTNodeResult.Succeeded, status);
        }

        [Test]
        public void AllAITest_ShouldWorkCorrectly()
        {
            // 初始化黑板和日志记录
            var bb = new Blackboard();
            var executionLog = new List<string>();

            // 构建完整行为树
            var behaviorTree = m_AIUnit.CreateBehaviorTree<BehaviorTree>(bb: bb);

            // 创建深度嵌套的节点结构
            IBTNode rootNode = new RepeaterBTNode()
            {
                data = new RepeaterBTNodeData()
                {
                    repeatMode = RepeaterBTNodeData.RepeatMode.CountLimited,
                    repeatCount = 2,
                    child = new SequenceBTNode()
                    {
                        data = new SequenceBTNodeData()
                        {
                            children = new IBTNode[]
                            {
                                new ParallelBTNode()
                                {
                                    data = new ParallelBTNodeData()
                                    {
                                        requireAllSuccess = true,
                                        children = new IBTNode[]
                                        {
                                            new ConditionBTNode()
                                            {
                                                data = new ConditionBTNodeData()
                                                {
                                                    condition = b => b.GetValue<bool>("CanStart")
                                                }
                                            },
                                            new DelayBTNode()
                                            {
                                                data = new DelayBTNodeData()
                                                {
                                                    duration = 0.3f,
                                                }
                                            },
                                            new ActionBTNode()
                                            {
                                                data = new ActionBTNodeData()
                                                {
                                                    callback = _ =>
                                                    {
                                                        executionLog.Add($"Wait Finish!");
                                                        return BTNodeResult.Succeeded;
                                                    }
                                                }
                                            },
                                        }

                                    }
                                },
                                new RepeaterBTNode()
                                {
                                    data = new RepeaterBTNodeData()
                                    { 
                                        repeatMode = RepeaterBTNodeData.RepeatMode.CountLimited,
                                        repeatCount = 1,
                                        child = new SequenceBTNode()
                                        {
                                            data = new SequenceBTNodeData()
                                            {
                                                children = new IBTNode[]
                                                {
                                                    new ActionBTNode()
                                                    {
                                                        data = new ActionBTNodeData()
                                                        {
                                                            callback = _ =>
                                                            {
                                                                bb.SetValue("Counter", bb.GetValue<int>("Counter") + 1);
                                                                executionLog.Add("CounterAction");
                                                                return BTNodeResult.Succeeded;
                                                            }
                                                        }
                                                    },
                                                    new ConditionBTNode()
                                                    {
                                                        data = new ConditionBTNodeData()
                                                        {
                                                            condition = b => b.GetValue<int>("Counter") > 0
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new ActionBTNode()
                                {
                                    data = new ActionBTNodeData()
                                    {
                                        callback = _ =>
                                        {
                                            executionLog.Add("FinalAction");
                                            return BTNodeResult.Succeeded;
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            };

            // 将节点添加到行为树
            behaviorTree.AddNode((RepeaterBTNode)rootNode);

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