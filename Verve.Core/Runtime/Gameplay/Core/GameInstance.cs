#if UNITY_5_3_OR_NEWER

namespace Verve.Gameplay
{
    /// <summary>
    /// 游戏实例 - 全局单例，负责管理游戏全局状态和系统
    /// </summary>
    public partial class GameInstance : ComponentInstanceBase<GameInstance>
    {
        public WorldContext Context { get; private set; }

        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            Context = new WorldContext(this);
        }
    }
}

#endif