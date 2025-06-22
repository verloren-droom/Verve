#if UNITY_5_3_OR_NEWER 

namespace VerveUniEx.Gameplay
{
    using Verve;
    using System;
    using UnityEngine;
    using System.Collections.Generic;


    public partial class GameState : ScriptableObject
    {
        [SerializeField, Tooltip("最大支持玩家数"), Min(0)] private int m_MaxPlayers = 1;
        [SerializeField, Tooltip("当前玩家数组"), ReadOnly] private List<PlayerController> m_Players = new List<PlayerController>();
        
        public IReadOnlyCollection<PlayerController> Players => m_Players.AsReadOnly();
        public bool IsFull => m_Players.Count >= m_MaxPlayers;
        
        public event Action<PlayerController> OnPlayerAdded;
        public event Action<PlayerController> OnPlayerRemoved;

        public int MaxPlayers
        {
            get => m_MaxPlayers;
            set => m_MaxPlayers = Mathf.Max(value, 0);
        }

        
        public void AddPlayer(PlayerController player)
        {
            if (IsFull || m_Players.Contains(player)) return;

            m_Players.Add(player);
            OnPlayerAdded?.Invoke(player);
        }
        
        public void RemovePlayer(PlayerController player)
        {
            if (!m_Players.Remove(player)) return;
            
            player.UnPossess();
            OnPlayerRemoved?.Invoke(player);
        }
    
        /// <summary>
        /// 重置游戏状态
        /// </summary>
        public virtual void ResetState()
        {
            foreach (var player in m_Players)
            {
                player.UnPossess();
                if (player.gameObject)
                    Destroy(player.gameObject);
            }
            m_Players.Clear();
        }
    }
}

#endif