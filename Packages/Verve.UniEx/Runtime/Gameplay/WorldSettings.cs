#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    /// 世界设置 - 场景特定的配置
    /// </summary>
    [Serializable]
    public partial class WorldSettings : ScriptableObject
    {
        [SerializeField, Tooltip("游戏模式")] private TypeReference<GameMode> m_GameMode;
        [SerializeField, Tooltip("游戏状态")] private TypeReference<GameState> m_GameState;
        [SerializeField, Tooltip("默认玩家控制预制体"), RequireComponentOnGameObject(typeof(PlayerController))] private GameObject m_DefaultPlayerControllerPrefab;
        [SerializeField, Tooltip("默认AI控制预制体"), RequireComponentOnGameObject(typeof(AIController))] private GameObject m_DefaultAIControllerPrefab;
        [SerializeField, Tooltip("默认角色预制体"), RequireComponentOnGameObject(typeof(Pawn))] private GameObject m_DefaultPawn;
        [SerializeField, Tooltip("最大支持玩家数"), Min(0)] private int m_MaxPlayerCount = 1;
        [SerializeField, Tooltip("最大支持AI数"), Min(0)] private int m_MaxAICount = 1;


        /// <summary> 默认玩家控制预制体 </summary>
        public GameObject DefaultPlayerControllerPrefab => m_DefaultPlayerControllerPrefab;
        /// <summary> 默认AI控制预制体 </summary>
        public GameObject DefaultAIControllerPrefab => m_DefaultAIControllerPrefab;
        /// <summary> 默认角色预制体 </summary>
        public GameObject DefaultPawnPrefab => m_DefaultPawn;
        /// <summary> 最大支持玩家数 </summary>
        public int MaxPlayerCount => m_MaxPlayerCount;
        /// <summary> 最大支持AI数 </summary>
        public int MaxAICount => m_MaxAICount;

        
        public T GetGameMode<T>() where T : GameMode
        {
            m_GameMode.Value ??= new GameMode();
            return m_GameMode.Value as T;
        }

        public T GetGameState<T>() where T : GameState
        {
            m_GameState.Value ??= new GameState();
            return m_GameState.Value as T;
        }
    }
}

#endif