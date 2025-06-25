#if UNITY_5_3_OR_NEWER 

namespace VerveUniEx.Gameplay
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;


    /// <summary>
    /// 游戏状态 - 存储游戏状态信息
    /// </summary>
    [Serializable]
    public partial class GameState
    {
        [SerializeField, Tooltip("玩家状态"), ReadOnly] private List<PlayerState> m_PlayerStates = new List<PlayerState>();
        
        public event Action<PlayerState> OnPlayerAdded;
        
        /// <summary> 当前玩家数组 </summary>
        public IReadOnlyCollection<PlayerState> PlayerStates => m_PlayerStates.AsReadOnly();


        public virtual void AddPlayerState(PlayerState state)
        {
            if (m_PlayerStates.Contains(state)) return;
            
            m_PlayerStates.Add(state);
            OnPlayerAdded?.Invoke(state);
        }

        public virtual void ResetState()
        {
            m_PlayerStates.Clear();
        }
    }

    
    /// <summary>
    /// 玩家状态 - 存储玩家状态信息
    /// </summary>
    [Serializable]
    public partial class PlayerState
    {
        public int playerId;
        public int score;
    }
}

#endif