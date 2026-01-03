#if UNITY_5_3_OR_NEWER

namespace Verve.Gameplay
{
    /// <summary>
    /// 世界上下文 - 提供当前世界的运行环境
    /// </summary>
    [System.Serializable]
    public class WorldContext
    {
        public GameInstance OwningGameInstance { get; }
        public World ActiveWorld { get; set; }
        public GameMode ActiveGameMode { get; set; }
        public GameState ActiveGameState { get; set; }
        
        public WorldContext(GameInstance gameInstance)
        {
            OwningGameInstance = gameInstance;
        }
    }
}

#endif