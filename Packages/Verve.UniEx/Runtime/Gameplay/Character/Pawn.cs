#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using UnityEngine;

    
    /// <summary>
    /// 控制实体基类
    /// </summary>
    public abstract class Pawn : MonoBehaviour
    {
        public Controller Controller { get; protected set; }
        
        
        /// <summary>
        /// 获取控制器
        /// </summary>
        /// <param name="controller"></param>
        public virtual void PossessedBy(Controller controller)
        {
            Controller = controller;
            controller.Possess(this);
        }
        
        /// <summary>
        /// 释放控制器
        /// </summary>
        public virtual void UnPossessed()
        {
            if (Controller != null) Controller.UnPossess();
            Controller = null;
        }
    }
}

#endif