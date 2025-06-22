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
    
        public virtual void Possess(Pawn pawn)
        {
            ControlledPawn = pawn;
            pawn.transform.SetParent(transform);
        }
    
        public virtual void UnPossess()
        {
            if (ControlledPawn != null)
            {
                ControlledPawn.transform.SetParent(null);
                ControlledPawn = null;
            }
        }
    }
}

#endif