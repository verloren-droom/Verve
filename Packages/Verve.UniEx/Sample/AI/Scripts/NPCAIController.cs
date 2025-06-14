using System;
using VerveEditor.UniEx.AI;

#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample
{
    using Verve.AI;
    using UnityEngine;
    using VerveUniEx.AI;
    
    
    public class NPCAIController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float m_MoveSpeed = 3f;
        [SerializeField] private Transform[] m_PatrolPoints;
        [SerializeField] private float m_StoppingDistance = 0.5f;

        [Header("Detection")]
        [SerializeField] private float m_SightRange = 10f;
        [SerializeField] private float m_LostSightTime = 5f;
        [SerializeField] private LayerMask m_TargetMask;

        [SerializeField] private BehaviorTree m_AI;
        private Blackboard m_BB;

        private Transform m_Target;

        private void Awake()
        {
            // m_Target = GameObject.FindWithTag("Player").transform;
            m_BB = new Blackboard();
            m_AI = GameLauncher.Instance.AI.CreateBT<BehaviorTree>(bb:  m_BB);
            BuildBehaviorTree();
        }

        private void BuildBehaviorTree()
        {
            var data = new TransformMoveBTNodeData()
            {
                owner = transform,
                targets = new Transform[] { m_BB.GetValue<Transform>("Target") },
                // Targets = new Func<Transform[]>(()
                //     => m_BB.TryGetValue<Transform>("Target", out var t) 
                //         ? new[] { t } 
                //         : Array.Empty<Transform>())(),
                // Targets = new Transform[] { m_Target },
                moveSpeed = m_MoveSpeed,
                minValidDistance = m_StoppingDistance,
                faceMovementDirection = true,
                rotationSpeed = 360f,
            };
            m_BB.SetValue("TransformMoveNode_1", data);

            var root = new SelectorBTNode()
            {
                data = new SelectorBTNodeData()
                {
                    children = new IBTNode[]
                    {
                        new SequenceBTNode()
                        {
                            data = new SequenceBTNodeData()
                            {
                                children = new IBTNode[]
                                {
                                    new VisionCheckBTNode()
                                    {
                                        data = new VisionCheckBTNodeData()
                                        {
                                            owner = transform,
                                            fieldOfViewAngle = 360f,
                                            eyeHeight = .25f,
                                            sightDistance = m_SightRange,
                                            targetLayerMask = m_TargetMask,
                                            targetTags = new string[] { "Player" },
                                            resultTargetKey = "Target",
                                            // Targets = new Transform[] { m_Target },
                                            detectionInterval = 0.1f,
                                        },
                                        IsDebug = true,
                                    },
                                    new ActionBTNode()
                                    {
                                        data = new ActionBTNodeData()
                                        {
                                            callback = bb =>
                                            {
                                                Debug.Log("Targets: ");
                                                data.targets = new Transform[] { m_BB.GetValue<Transform>("Target") };
                                                bb.SetValue("TransformMoveNode_1", data);
                                                return BTNodeResult.Succeeded;
                                            }
                                        }
                                    },
                                    new TransformMoveBTNode()
                                    {
                                        dataKey = "TransformMoveNode_1",
                                        data = data,
                                    },
                                    new ActionBTNode()
                                    {
                                        data = new ActionBTNodeData()
                                        {
                                            callback = bb =>
                                            {
                                                Debug.Log("Chasing!");
                                                data.targets = new Transform[] { };
                                                bb.SetValue("TransformMoveNode_1", data);
                                                return BTNodeResult.Succeeded;
                                            }
                                        }
                                    },
                                }
                            }
                        },
                        new SequenceBTNode()
                        {
                            data = new SequenceBTNodeData()
                            {
                                children = new IBTNode[]
                                {
                                    new DelayBTNode()
                                    {
                                        data = new DelayBTNodeData()
                                        {
                                            duration = m_LostSightTime
                                        }
                                    },
                                    new TransformMoveBTNode()
                                    {
                                        data = new TransformMoveBTNodeData()
                                        {
                                            owner = transform,
                                            targets = m_PatrolPoints,
                                            moveSpeed = m_MoveSpeed,
                                            minValidDistance = m_StoppingDistance,
                                            faceMovementDirection = true,
                                            rotationSpeed = 360f,
                                            loopMode = LoopMode.PingPong,
                                            arrivalMode = ArrivalActionMode.Keep,
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            m_AI.AddNode(root);
        }

        private void OnDrawGizmos()
        {
            m_AI.DrawGizmos();
        }
    }
}

#endif