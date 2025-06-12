#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using VerveUniEx.AI;
    using UnityEngine.AI;
    using System.Collections;
    using VerveEditor.UniEx.AI;
    
    
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
            var root = new SelectorNode()
            {
                Data = new SelectorNodeData()
                {
                    Children = new IBTNode[]
                    {
                        new SequenceNode()
                        {
                            Data = new SequenceNodeData()
                            {
                                Children = new IBTNode[]
                                {
                                    new VisionCheckNode()
                                    {
                                        Key = "Vision123",
                                        // IsPreparable = true,
                                        Data = new VisionCheckNodeData()
                                        {
                                            Owner = transform,
                                            FieldOfViewAngle = 360f,
                                            EyeHeight = .8f,
                                            SightDistance = m_SightRange,
                                            TargetLayerMask = m_TargetMask,
                                            TargetTags = new string[] { "Player" },
                                            ResultTargetKey = "Target",
                                            // Targets = new Transform[] { m_Target },
                                            DetectionInterval = 0.1f,
                                        },
                                        IsDebug = true,
                                    },
                                    new TransformMoveNode()
                                    {
                                        Data = new TransformMoveNodeData()
                                        {
                                            Owner = transform,
                                            Targets = new Transform[] { m_BB.GetValue<Transform>("Target") },
                                            // Targets = new Func<Transform[]>(()
                                            //     => m_BB.TryGetValue<Transform>("Target", out var t) 
                                            //         ? new[] { t } 
                                            //         : Array.Empty<Transform>())(),
                                            // Targets = new Transform[] { m_Target },
                                            MoveSpeed = m_MoveSpeed,
                                            MinValidDistance = m_StoppingDistance,
                                            FaceMovementDirection = true,
                                            RotationSpeed = 360f,
                                        }, 
                                    }
                                }
                            }
                        },
                        new SequenceNode()
                        {
                            Data = new SequenceNodeData()
                            {
                                Children = new IBTNode[]
                                {
                                    new DelayNode()
                                    {
                                        Data = new DelayNodeData()
                                        {
                                            Duration = m_LostSightTime
                                        }
                                    },
                                    new TransformMoveNode()
                                    {
                                        Data = new TransformMoveNodeData()
                                        {
                                            Owner = transform,
                                            Targets = m_PatrolPoints,
                                            MoveSpeed = m_MoveSpeed,
                                            MinValidDistance = m_StoppingDistance,
                                            FaceMovementDirection = true,
                                            RotationSpeed = 360f,
                                            Loop = TransformMoveNodeData.LoopMode.PingPong,
                                            ArrivalMode = TransformMoveNodeData.ArrivalActionMode.Keep,
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