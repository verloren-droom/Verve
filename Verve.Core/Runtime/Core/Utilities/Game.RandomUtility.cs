namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>随机工具类</para>
        /// </summary>
        public static class RandomUtility
        {
            [ThreadStatic] private static readonly Random s_Random = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
            
            /// <summary>
            ///   <para>获取一个0-1的随机浮点数</para>
            /// </summary>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float RandFloat() => (float)s_Random.NextDouble();
            
            /// <summary>
            ///   <para>生成 [min, max] 范围内的随机浮点数</para>
            /// </summary>
            /// <param name="min">最小值</param>
            /// <param name="max">最大值</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float RandRange(float min, float max)
            {
                if (min > max)
                    throw new ArgumentException("min must be less than or equal to max");
                return min + (float)s_Random.NextDouble() * (max - min);
            }

            /// <summary>
            ///   <para>获取一个32位随机整数</para>
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int RandInt32() => s_Random.Next();

            /// <summary>
            ///   <para>获取一个64位随机整数</para>
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static long RandInt64() => ((long)(uint)s_Random.Next() << 32) | (uint)s_Random.Next();
            
            /// <summary>
            ///   <para>生成 [min, max) 范围内的随机整数</para>
            /// </summary>
            /// <param name="min">最小值（包含）</param>
            /// <param name="max">最大值（不包含）</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int RandRange(int min, int max) => s_Random.Next(min, max);
            
            /// <summary>
            ///   <para>生成随机布尔值</para>
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool RandBool() => s_Random.Next(2) != 0;
        }
    }
}