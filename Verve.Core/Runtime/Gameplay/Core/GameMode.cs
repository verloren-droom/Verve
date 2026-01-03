#if UNITY_5_3_OR_NEWER

namespace Verve.Gameplay
{
    using System;
    using UnityEngine;

    
    /// <summary>
    /// 游戏模式 - 定义游戏规则
    /// </summary>
    [Serializable]
    public partial class GameMode
    {
        public virtual void InitGame(World world)
        {
            SpawnPlayer(world);
            SpawnAI(world);
        }
        
        /// <summary> 生成玩家 </summary>
        public virtual PlayerController SpawnPlayer(World world, PlayerStart spawnPoint = null)
        {
            if (world.Settings.DefaultPlayerControllerPrefab == null || world.IsFullPlayer) return null;
            
            spawnPoint ??= PlayerStart.FindValidPlayerStart();
            if (spawnPoint == null) return null;
            
            var playerController = UnityEngine.Object.Instantiate(world.Settings.DefaultPlayerControllerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, world.transform).GetComponent<PlayerController>();
            world.RegisterPlayer(playerController);
            playerController.Possess(playerController.GetComponent<PlayerPawn>());
            spawnPoint.IsOccupied = true;

            return playerController;
        }
        
        /// <summary> 生成AI </summary>
        public virtual AIController SpawnAI(World world, PatrolPath patrolPath = null)
        {
            if (world.Settings.DefaultAIControllerPrefab == null || world.IsFullAI) return null;

            patrolPath ??= GameObject.FindObjectOfType<PatrolPath>();
            if (patrolPath == null) return null;

            var aiController = UnityEngine.Object.Instantiate(world.Settings.DefaultAIControllerPrefab, patrolPath.GetWorldPosition(0), patrolPath.transform.rotation, world.transform).GetComponent<AIController>();
            world.RegisterAI(aiController);
            aiController.SetPatrolPath(patrolPath);
            aiController.Possess(aiController.GetComponent<AIPawn>());

            return aiController;
        }
    }
}

#endif