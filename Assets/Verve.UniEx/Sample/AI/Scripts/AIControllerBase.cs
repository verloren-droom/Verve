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
            AI = GameLauncher.Instance.AI.CreateBT<BehaviorTree>(m_InitialTreeCapacity, BB);
            
            BuildBehaviorTree();
        }
    
        protected abstract void BuildBehaviorTree();

        protected virtual void OnDestroy()
        {
            GameLauncher.Instance.AI.DestroyBT(AI.ID);
        }
    }
}

#endif