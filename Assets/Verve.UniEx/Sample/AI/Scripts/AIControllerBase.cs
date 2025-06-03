#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample
{
    using Verve.AI;
    using UnityEngine;
    
    
    public abstract class AIControllerBase : MonoBehaviour
    {
        protected BehaviorTree AI { get; private set; }
        protected Blackboard BB { get; private set; }
        
        [Header("AI Settings")]
        [SerializeField] private int m_InitialBlackboardSize = 32;
        [SerializeField] private int m_InitialTreeCapacity = 64;

        
        protected virtual void Awake()
        {
            BB = new Blackboard(m_InitialBlackboardSize);
            AI = GameLauncher.Instance.AI.CreateTree<BehaviorTree>(m_InitialTreeCapacity, BB);
            
            BuildBehaviorTree();
        }
    
        protected abstract void BuildBehaviorTree();
    
        protected virtual void Update()
        {
            (AI as IBehaviorTree).Update(Time.deltaTime);
        }
    
        protected virtual void OnDestroy()
        {
            AI.Dispose();
        }
    
        protected T AddNode<T>(T node) where T : struct, IAINode
        {
            AI.AddNode(node);
            return node;
        }
    
        protected SequenceNode NewSequence(params IAINode[] children)
        {
            return new SequenceNode { Children = children };
        }
    
        protected ParallelNode NewParallel(params IAINode[] children)
        {
            return new ParallelNode { Children = children };
        }
    }
}

#endif