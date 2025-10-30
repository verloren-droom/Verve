#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Runtime.CompilerServices;
    

    /// <summary>
    ///   <para>游戏功能上下文</para>
    ///   <para>存放游戏功能执行所需的上下文信息</para>
    /// </summary>
    [Serializable]
    public readonly struct GameFeatureContext : IGameFeatureContext
    {
        /// <summary>
        ///   <para>增量帧时间</para>
        /// </summary>
        public readonly float DeltaTime;
        /// <summary>
        ///   <para>运行总时间</para>
        /// </summary>
        public readonly float Time;
        /// <summary>
        ///   <para>运行帧数</para>
        /// </summary>
        public readonly int FrameCount;
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameFeatureContext(in float dt, in float time, in int frameCount)
        {
            DeltaTime = dt;
            Time = time;
            FrameCount = frameCount;
        }
        
        /// <summary>
        ///   <para>默认游戏功能上下文</para>
        /// </summary>
        public static GameFeatureContext Default
            => new GameFeatureContext(UnityEngine.Time.deltaTime, UnityEngine.Time.time, UnityEngine.Time.frameCount);
    }
}

#endif