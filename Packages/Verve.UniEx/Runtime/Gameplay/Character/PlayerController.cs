#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Gameplay
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 玩家控制器 - 玩家控制
    /// </summary>
    public partial class PlayerController : Controller
    {
        public Camera PlayerCamera => ControlledPawn is PlayerPawn playerPawn ? playerPawn.PlayerCamera : null;
        public PlayerMovement Movement => ControlledPawn is PlayerPawn playerPawn ? playerPawn.Movement : null;
    }
}

#endif