#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using UnityEngine;
    
    
    /// <summary>
    /// 控制基类
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class Controller : MonoBehaviour
    {
        public Pawn ControlledPawn { get; private set; }
    
        
        public void Possess(Pawn pawn)
        {
            ControlledPawn = pawn;
            pawn.transform.SetParent(transform);
            OnPossess();
        }
    
        public void UnPossess()
        {
            if (ControlledPawn != null)
            {
                ControlledPawn.transform.SetParent(null);
                ControlledPawn = null;
            }
            OnUnPossess();
        }
        
        protected virtual void OnPossess() { }
        protected virtual void OnUnPossess() { }
    }
}

#endif