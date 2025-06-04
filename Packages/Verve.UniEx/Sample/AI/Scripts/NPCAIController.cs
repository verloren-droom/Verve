#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample
{
    using AI;
    using Verve.AI;
    using UnityEngine;
    using UnityEngine.AI;
    
    
    public class NPCAIController : MonoBehaviour
    {
        protected BehaviorTree AI { get; private set; }
        protected Blackboard BB { get; private set; }
        
        [Header("AI Settings")]
        [SerializeField] private int m_InitialBlackboardSize = 32;
        [SerializeField] private int m_InitialTreeCapacity = 64;
        
        [Header("Movement")]
        [SerializeField] private float m_MoveSpeed = 3f;
        [SerializeField] private Transform[] m_PatrolPoints;
        [SerializeField, Min(0.1f)] private float m_PauseDuration = 2f;
        private int m_CurrentPatrolIndex;
        private float m_PauseTimer;

        [Header("Detection")] 
        [SerializeField] private float m_SightRange = 10f;
        [SerializeField] private float m_LostSightTime = 5f; 
        [SerializeField] private LayerMask m_DetectionMask;
        private float m_LastSightTime;
        private Transform m_Player;

        [Header("States")]
        [SerializeField] private AIState m_CurrentState = AIState.Patrol;

        [SerializeField] private TransformMoveNode.ArrivalActionMode m_ArrivalMode;
        [SerializeField] private TransformMoveNode.LoopMode m_LoopMode;
        [SerializeField] private TransformMoveNode.AxisFlags m_IgnoreAxes;

        [SerializeField] private NavMeshAgent m_Agent;
        
        public enum AIState { Patrol, Pause, Chase, Search }

        private void Awake()
        {
            m_Player = GameObject.FindWithTag("Player")?.transform;
            BB = new Blackboard(m_InitialBlackboardSize);
            AI = GameLauncher.Instance.AI.CreateBT<BehaviorTree>(m_InitialTreeCapacity, BB);
            BB.SetValue("Controller", this);
            
            BuildBehaviorTree();
        }

        private void BuildBehaviorTree()
        {
            var stateSelector = new SelectorNode() 
            {
                Children = new IBTNode[]
                {
                    // 1. 追击状态（最高优先级）
                    new SequenceNode()
                    {
                        Children = new IBTNode[]
                        {
                            new ConditionNode { Condition = _ => m_CurrentState == AIState.Chase },
                            BuildChaseBehavior()
                        }
                    },
                    
                    // 2. 搜索状态（第二优先级）
                    new SequenceNode()
                    {
                        Children = new IBTNode[]
                        {
                            new ConditionNode { Condition = _ => m_CurrentState == AIState.Search },
                            BuildSearchBehavior()
                        }
                    },
                    
                    // 3. 暂停状态
                    new SequenceNode()
                    {
                        Children = new IBTNode[]
                        {
                            new ConditionNode { Condition = _ => m_CurrentState == AIState.Pause },
                            BuildPauseBehavior()
                        }
                    },
                    
                    // 4. 默认巡逻状态（最低优先级）
                    new SequenceNode()
                    {
                        Children = new IBTNode[]
                        {
                            new ConditionNode { Condition = _ => m_CurrentState == AIState.Patrol },
                            BuildPatrolBehavior()
                        }
                    }
                }
            };

            var detectionSystem = new ActionNode { Callback = _ => UpdateDetectionState() };

            AI.AddNode(new ParallelNode
            {
                Children = new IBTNode[]
                {
                    stateSelector,
                    detectionSystem
                }
            });
        }
    
        #region 行为构建方法
        private IBTNode BuildPatrolBehavior()
        {
            return new SequenceNode()
            {
                Children = new IBTNode[]
                {
                    new TransformMoveNode
                    {
                        Owner = transform,
                        Targets = m_PatrolPoints,
                        MoveSpeed = m_MoveSpeed,
                        MinValidDistance = 0.5f,
                        FaceMovementDirection = true,
                        RotationSpeed = 180f,
                        ArrivalMode = m_ArrivalMode,
                        Loop = m_LoopMode,
                        IgnoreAxes = m_IgnoreAxes,
                    },
                    new ChangeStateNode() { TargetState = AIState.Pause }
                    // new NavMeshMoveNode 
                    // {
                    //     Agent = m_Agent,
                    //     Targets = m_PatrolPoints,
                    //     AutoRotation = true,
                    //     RotationSpeed = 120f,
                    //     ArrivalMode = m_ArrivalMode,
                    //     Loop = m_LoopMode,
                    // }
                }
            };
        }
    
        private IBTNode BuildPauseBehavior()
        {
            return new SequenceNode()
            {
                Children = new IBTNode[]
                {
                    new WaitNode { Duration = m_PauseDuration },
                    new ChangeStateNode() { TargetState = AIState.Patrol }
                }
            };
        }
    
        private IBTNode BuildChaseBehavior()
        {
            return new SequenceNode()
            {
                Children = new IBTNode[]
                {
                    new TransformMoveNode
                    {
                        Owner = transform,
                        Targets = new[] { m_Player },
                        MoveSpeed = m_MoveSpeed * 1.5f,
                        MinValidDistance = 1f,
                        FaceMovementDirection = true,
                        RotationSpeed = 360f,
                    },
                    new ConditionNode { Condition = CheckPlayerLost }
                }
            };
        }
    
        private IBTNode BuildSearchBehavior()
        {
            return new SequenceNode()
            {
                Children = new IBTNode[]
                {
                    new TransformMoveNode {
                        Owner = transform,
                        Targets = new[] { BB.GetValue<Transform>("LastKnownPosition") },
                        MoveSpeed = m_MoveSpeed,
                        MinValidDistance = 0.5f,
                        FaceMovementDirection = true
                    }
                }
            };
        }
        #endregion
    
        public struct ChangeStateNode : IBTNode 
        {
            public AIState TargetState;
            
            NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
            {
                bb.GetValue<NPCAIController>("Controller").m_CurrentState = TargetState;
                return NodeStatus.Success;
            }
        }
    
        #region 状态行为方法
        private bool CheckPlayerLost(Blackboard bb)
        {
            if (Time.time - m_LastSightTime > m_LostSightTime)
            {
                m_CurrentState = AIState.Search;
                BB.SetValue("LastKnownPosition", m_Player.position);
                return true;
            }
            return false;
        }

        private NodeStatus SearchLastKnownPosition(Blackboard bb)
        {
            var lastPos = BB.GetValue<Vector3>("LastKnownPosition");
            transform.position = Vector3.MoveTowards(
                transform.position,
                lastPos,
                m_MoveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, lastPos) < 1f)
            {
                m_CurrentState = AIState.Patrol;
                return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }
        #endregion

        #region 检测系统
        private NodeStatus UpdateDetectionState()
        {
            if (m_Player == null) return NodeStatus.Failure;

            bool canSeePlayer = Physics.Raycast(
                transform.position,
                (m_Player.position - transform.position).normalized,
                out var hit,
                m_SightRange,
                m_DetectionMask
            ) && hit.transform == m_Player;

            if (canSeePlayer)
            {
                m_LastSightTime = Time.time;
                if (m_CurrentState != AIState.Chase)
                {
                    m_CurrentState = AIState.Chase;
                }
            }
            
            return NodeStatus.Success;
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            // 绘制视野范围
            Gizmos.color = m_CurrentState == AIState.Chase ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_SightRange);

            // 绘制最后已知位置
            if (m_CurrentState == AIState.Search)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(BB.GetValue<Vector3>("LastKnownPosition"), 0.5f);
            }

            for (int index = 0; index < m_PatrolPoints.Length; index++)
            {
                if (m_PatrolPoints[index] != null)
                {
                    Gizmos.DrawLine(m_PatrolPoints[index].position, m_PatrolPoints[Mathf.Min(m_PatrolPoints.Length - 1,index + 1)].position);
                }
            }
        }
    }
}

#endif