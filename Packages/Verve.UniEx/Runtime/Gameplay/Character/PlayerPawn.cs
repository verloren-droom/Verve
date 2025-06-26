#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using UnityEngine;
    
    
    /// <summary>
    /// 玩家控制的实体基类
    /// </summary>
    [RequireComponent(typeof(PlayerMovement)), DisallowMultipleComponent, AddComponentMenu("Verve/Gameplay/Character/PlayerPawn")]
    public partial class PlayerPawn : Pawn
    {
        [Header("玩家组件")]
        [SerializeField, Tooltip("跟随玩家的摄像机")] private Camera m_PlayerCamera;
        [SerializeField, Tooltip("玩家移动组件")] private PlayerMovement m_Movement;

        public Camera PlayerCamera
        {
            get => m_PlayerCamera;
            set => m_PlayerCamera = value;
        }

        public PlayerMovement Movement
        {
            get => m_Movement;
            set => m_Movement = value;
        }

        
        /// <summary>
        /// 被玩家控制器控制时调用
        /// </summary>
        public override void PossessedBy(Controller controller)
        {
            base.PossessedBy(controller);
            m_Movement ??= GetComponent<PlayerMovement>();
            m_PlayerCamera ??= GetComponentInChildren<Camera>() ?? Camera.main;
        }
    }
}

#endif