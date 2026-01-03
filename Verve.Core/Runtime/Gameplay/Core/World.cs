#if UNITY_5_3_OR_NEWER

namespace Verve.Gameplay
{
    using Verve;
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    
    
    /// <summary>
    ///  世界 - 管理当前游戏世界，每个游戏世界都对应一个世界对象
    /// </summary>
    [Serializable]
    public partial class World : MonoBehaviour
    {
        [SerializeField, Tooltip("游戏世界配置")] private WorldSettings m_Settings;
        [SerializeField, Tooltip("当前玩家数组"), ReadOnly] private List<PlayerController> m_Players = new List<PlayerController>();
        [SerializeField, Tooltip("当前AI数组"), ReadOnly] private List<AIController> m_AIs = new List<AIController>();
        
        private WorldContext m_Context;
        
        /// <summary> 当前玩家数组 </summary>
        public IReadOnlyCollection<PlayerController> Players => m_Players.AsReadOnly();
        /// <summary> 当前AI数组 </summary>
        public IReadOnlyCollection<AIController> AIs => m_AIs.AsReadOnly();
        public WorldSettings Settings => m_Settings;

        /// <summary> 是否满玩家 </summary>
        public bool IsFullPlayer => m_Players.Count >= Settings.MaxPlayerCount;
        /// <summary> 是否满AI </summary>
        public bool IsFullAI => m_AIs.Count >= Settings.MaxAICount;

        
        private void Awake()
        {
            Initialize();
        }
        
        protected virtual void Initialize()
        {
            m_Settings ??= ScriptableObject.CreateInstance<WorldSettings>();
            m_Context = GameInstance.Instance.Context;
            m_Context.ActiveWorld = this;
            m_Context.ActiveGameMode = m_Settings.GetGameMode<GameMode>();
            m_Context.ActiveGameState = m_Settings.GetGameState<GameState>();
        }
        
        private IEnumerator Start()
        {
            yield return null;
            m_Context.ActiveGameMode?.InitGame(this);
        }

        /// <summary>
        /// 添加玩家到世界
        /// </summary>
        public bool RegisterPlayer(PlayerController player)
        {
            CleanupDestroyedPlayers();
            if (m_Players.Contains(player) || IsFullPlayer) { return false; }
            
            m_Players.Add(player);
            // m_Context.ActiveGameState?.AddPlayerState(new PlayerState());
            return true;
        }

        /// <summary>
        /// 添加AI到世界
        /// </summary>
        public void RegisterAI(AIController ai)
        {
            CleanupDestroyedAIs();
            if (m_AIs.Contains(ai) || IsFullAI) { return; }
            m_AIs.Add(ai);
        }

        /// <summary> 清理已被销毁的玩家引用 </summary>
        private void CleanupDestroyedPlayers()
        {
            m_Players.RemoveAll(pc => pc == null);
        }

        /// <summary> 清理已被销毁的AI引用 </summary>
        private void CleanupDestroyedAIs()
        {
            m_AIs.RemoveAll(ai => ai == null);
        }
    }
}

#endif