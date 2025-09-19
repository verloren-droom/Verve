#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Runtime.CompilerServices;
    

    /// <summary>
    /// 游戏功能上下文 - 存放游戏功能执行所需的上下文信息
    /// </summary>
    [Serializable]
    public readonly struct GameFeatureContext : IGameFeatureContext
    {
        /// <summary> 增量帧时间 </summary>
        public readonly float DeltaTime;
        /// <summary> 运行总时间 </summary>
        public readonly float Time;
        /// <summary> 帧数 </summary>
        public readonly int FrameCount;
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameFeatureContext(in float dt, in float time, in int frameCount)
        {
            DeltaTime = dt;
            Time = time;
            FrameCount = frameCount;
        }
        
        /// <summary> 默认上下文 </summary>
        public static GameFeatureContext Default
            => new GameFeatureContext(UnityEngine.Time.deltaTime, UnityEngine.Time.time, UnityEngine.Time.frameCount);
    }
}

#endif