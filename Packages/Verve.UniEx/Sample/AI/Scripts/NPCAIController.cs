#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using VerveUniEx.AI;
    using UnityEngine.AI;
    using System.Collections;
    
    
    public class NPCAIController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float m_MoveSpeed = 3f;
        [SerializeField] private Transform[] m_PatrolPoints;
        [SerializeField] private float m_StoppingDistance = 0.5f;

        [Header("Detection")]
        [SerializeField] private float m_SightRange = 10f;
        [SerializeField] private float m_LostSightTime = 5f;
        [SerializeField] private LayerMask m_DetectionMask;

        // 行为树组件
        [SerializeField] private BehaviorTree m_AI;
        private Blackboard m_BB;
        private Transform m_Player;

        
        private void Awake()
        {
            InitializeComponents();
            BuildBehaviorTree();
        }

        private void InitializeComponents()
        {
            m_Player = GameObject.FindGameObjectWithTag("Player")?.transform;
            m_BB = new Blackboard(256);
            m_AI = GameLauncher.Instance.AI.CreateBT<BehaviorTree>();
        
            // 初始化黑板数据
            m_BB.SetValue("IsPlayerVisible", false);
            // m_BB.SetValue("LastKnownPosition", m_Player);
            m_BB.SetValue("LastSightTime", -1f);
        }
        
        private void BuildBehaviorTree()
        {
            // 根节点为并行执行感知系统和行为系统
            var root = new ParallelNode
            {
                Children = new IBTNode[]
                {
                    CreatePerceptionSystem(),
                    CreateBehaviorSystem()
                }
            };
            
            m_AI.AddNode(root);
        }
        
        private IBTNode CreatePerceptionSystem()
        {
            return new RepeaterNode() // 使用循环节点包裹检测逻辑
            {
                Child = new SequenceNode
                {
                    Children = new IBTNode[]
                    {
                        new DelayNode { Duration = 0.2f },
                        new ConditionNode
                        {
                            Condition = CheckPlayerVisibility
                        },
                        new ActionNode { Callback = UpdateLastKnownPosition }
                    }
                }
            };
        }
        
        private IBTNode CreateBehaviorSystem()
        {
            return new SelectorNode
            {
                Children = new IBTNode[]
                {
                    // 最高优先级：追逐可见玩家
                    CreateChaseBehavior(),
                    // 第二优先级：搜索最后已知位置
                    CreateSearchBehavior(),
                    // 默认行为：巡逻
                    CreatePatrolBehavior(),
                }
            };
        }

        private IBTNode CreateChaseBehavior()
        {
            return new SequenceNode
            {
                Children = new IBTNode[]
                {
                    new ConditionNode
                    {
                        Condition = bb => bb.GetValue<bool>("IsPlayerVisible")
                    },
                    new TransformMoveNode
                    {
                        Owner = transform,
                        Targets = new[] { m_Player },
                        MoveSpeed = m_MoveSpeed * 1.5f,
                        MinValidDistance = m_StoppingDistance,
                        FaceMovementDirection = true,
                        IsDebug = true,
                        DebugTarget = gameObject
                    },
                    new ConditionNode
                    {
                        Condition = CheckPlayerLost
                    }
                }
            };
        }
        
        private IBTNode CreateSearchBehavior()
        {
            return new SequenceNode
            {
                Children = new IBTNode[]
                {
                    new ConditionNode
                    {
                        Condition = bb => Time.time - bb.GetValue<float>("LastSightTime") < m_LostSightTime
                    },
                    new TransformMoveNode
                    {
                        Owner = transform,
                        Targets = new[] { m_BB.GetValue<Transform>("LastKnownPosition") },
                        MoveSpeed = m_MoveSpeed,
                        MinValidDistance = m_StoppingDistance,
                        IsDebug = true,
                        DebugTarget = gameObject,
                    },
                    new DelayNode { Duration = 5f },
                    new ActionNode { Callback = ResetSearchState },
                }
            };
        }

        private IBTNode CreatePatrolBehavior()
        {
            return new SequenceNode
            {
                Children = new IBTNode[]
                {
                    new TransformMoveNode
                    {
                        Owner = transform,
                        Targets = m_PatrolPoints,
                        MoveSpeed = m_MoveSpeed,
                        MinValidDistance = m_StoppingDistance,
                        ArrivalMode = TransformMoveNode.ArrivalActionMode.Keep,
                        Loop = TransformMoveNode.LoopMode.PingPong,
                        IsDebug = true,
                        DebugTarget = gameObject
                    },
                    new DelayNode { Duration = 2f } // 巡逻点停留
                }
            };
        }

        private bool CheckPlayerVisibility(Blackboard bb)
        {
            if (m_Player == null) return false;
        
            var direction = m_Player.position - transform.position;
            if (direction.magnitude > m_SightRange) return false;
            
        
            if (Physics.Raycast(transform.position, direction.normalized, 
                out var hit, m_SightRange, m_DetectionMask))
            {
                bool isVisible = hit.transform == m_Player;
                m_BB.SetValue("IsPlayerVisible", isVisible);
                
                if (isVisible)
                {
                    m_BB.SetValue("LastSightTime", Time.time);
                }
                return true;
            }
            return false;
        }
        
        private NodeStatus UpdateLastKnownPosition(Blackboard bb)
        {
            if (bb.GetValue<bool>("IsPlayerVisible"))
            {
                bb.SetValue("LastKnownPosition", m_Player);
            }
            return NodeStatus.Success;
        }

        private bool CheckPlayerLost(Blackboard bb)
        {
            return !bb.GetValue<bool>("IsPlayerVisible") && 
                  (Time.time - bb.GetValue<float>("LastSightTime")) > m_LostSightTime;
        }
        
        private NodeStatus ResetSearchState(Blackboard bb)
        {
            bb.SetValue("LastSightTime", 0f);
            return NodeStatus.Success;
        }
    }
}

#endif