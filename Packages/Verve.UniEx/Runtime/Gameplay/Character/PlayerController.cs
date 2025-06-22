#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 玩家角色控制基类
    /// </summary>
    public partial class PlayerController : Controller
    {
        public Camera PlayerCamera => ControlledPawn is PlayerPawn playerPawn ? playerPawn.PlayerCamera : null;
        public PlayerMovement Movement => ControlledPawn is PlayerPawn playerPawn ? playerPawn.Movement : null;
    }
}

#endif