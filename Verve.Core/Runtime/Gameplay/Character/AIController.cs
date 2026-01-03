#if UNITY_5_3_OR_NEWER

namespace Verve.Gameplay
{
    using AI;
    using UnityEngine;
    using System.Collections.Generic;

    
    /// <summary>
    /// AI角色控制基类
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Verve/Gameplay/Character/AIController")]
    public partial class AIController : Controller
    {
        [SerializeField, Tooltip("巡逻路径")] private PatrolPath m_PatrolPath;
        
        
        public BehaviorTree BT { get; protected set; }
        

        protected override void OnPossess()
        {
            base.OnPossess();
            
            BT = new BehaviorTree();
        }
        
        
        public void SetPatrolPath(PatrolPath patrolPath) => m_PatrolPath = patrolPath;
    }
}

#endif