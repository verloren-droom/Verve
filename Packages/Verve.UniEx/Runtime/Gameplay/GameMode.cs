#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using Verve;
    using System;
    using Verve.Pool;
    using System.Linq;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    
    public partial class GameMode : ScriptableObject
    {
        [SerializeField, Tooltip("默认游戏状态")] private GameState m_DefaultGameState;
        [SerializeField, Tooltip("默认玩家角色"), RequireComponentOnGameObject(typeof(PlayerPawn))] private GameObject m_DefaultPlayerPawn;
        [SerializeField, Tooltip("默认AI角色"), RequireComponentOnGameObject(typeof(AIPawn))] private GameObject m_DefaultAIPawn;

        public GameState DefaultGameState => m_DefaultGameState;
        public GameObject DefaultPlayerPawn => m_DefaultPlayerPawn;
        public GameObject DefaultAIPawn => m_DefaultAIPawn;

        /// <summary> 游戏初始化事件 </summary>
        public event Action OnGameInited;
        /// <summary> 游戏开始事件 </summary>
        public event Action OnGameStarted;
        /// <summary> 游戏结束事件 </summary>
        public event Action OnGameEnded;
        /// <summary> 玩家生成事件 </summary>
        public event Action<PlayerController> OnPlayerSpawned;
        
        private ObjectPool<PlayerController> m_PlayerPool;


        public virtual void InitGame()
        {
            m_PlayerPool?.Clear();
            m_PlayerPool = new ObjectPool<PlayerController>(() =>
            {
                var obj = Instantiate(
                    m_DefaultPlayerPawn,
                    Vector3.zero,
                    Quaternion.identity
                ).GetComponent<PlayerController>();
                obj.gameObject.SetActive(false);
                return obj;
            }, controller =>
            {
                controller.gameObject.SetActive(true);
            }, controller =>
            {
                controller.gameObject.SetActive(false);
                controller.UnPossess();
            }, controller =>
            {
                Destroy(controller.gameObject);
            }, m_DefaultGameState.MaxPlayers);
            OnGameInited?.Invoke();
        }


        /// <summary>
        /// 开始游戏
        /// </summary>
        public virtual void StartGame()
        {
            SpawnPlayer();
            SpawnAI();
            OnGameStarted?.Invoke();
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public virtual void EndGame()
        {
            CleanupPlayers();
            CleanupAI();
            OnGameEnded?.Invoke();
        }

        /// <summary>
        /// 生成玩家
        /// </summary>
        public virtual PlayerController SpawnPlayer(PlayerStart spawnPoint = null)
        {
            if (m_DefaultPlayerPawn == null) return null;
            
            spawnPoint ??= FindValidSpawnPoint();
            
            if (spawnPoint == null) return null;
            if (!m_PlayerPool.TryGet(out var playerController)) return null;
            
            playerController.transform.position = spawnPoint.transform.position;
            playerController.transform.rotation = spawnPoint.transform.rotation;

            spawnPoint.IsOccupied = true;
            m_DefaultGameState.AddPlayer(playerController);
            OnPlayerSpawned?.Invoke(playerController);
            return playerController;
        }
        
        /// <summary>
        /// 重生玩家
        /// </summary>
        public virtual IEnumerator RespawnPlayer(PlayerController player, float delay = 0f)
        {
            yield return new WaitForSeconds(delay);
            var oldStart = PlayerStart.AllPlayerStarts.FirstOrDefault(s => s.IsOccupied);
            if (oldStart != null) oldStart.IsOccupied = false;

            m_DefaultGameState.RemovePlayer(player);
            m_PlayerPool.Release(player);

            SpawnPlayer();
        }
        
        /// <summary>
        /// 查找有效出生点
        /// </summary>
        protected virtual PlayerStart FindValidSpawnPoint()
        {
            return PlayerStart.AllPlayerStarts
                .FirstOrDefault(start => 
                    start.CheckIsValidSpawnPoint() && 
                    !start.IsOccupied);
        }
        
        protected virtual void CleanupPlayers()
        {
            for (int i = 0; i < m_DefaultGameState.Players.Count; i++)
            {
                var player = m_DefaultGameState.Players.ElementAt(i);
                player.UnPossess();
                m_DefaultGameState.RemovePlayer(player);
            }
            
            m_PlayerPool.Clear();
        }
        
        /// <summary>
        /// 生成AI
        /// </summary>
        protected virtual AIController SpawnAI(PlayerStart spawnPoint = null)
        {
            return null;
        }

        /// <summary>
        /// 清理AI
        /// </summary>
        protected virtual void CleanupAI()
        {
            // TODO: 
        }
    }
}

#endif