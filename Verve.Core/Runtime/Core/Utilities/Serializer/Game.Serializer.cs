namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        [ThreadStatic] private static ISerializer s_Serializer = JsonSerializer.Instance;
        
        /// <summary>
        ///   <para>序列化工具</para>
        /// </summary>
        public static ISerializer Serializer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Serializer ??= JsonSerializer.Instance;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => s_Serializer = value ?? JsonSerializer.Instance;
        }
    }
}