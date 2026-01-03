namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        [ThreadStatic] private static ISerializable s_Serializable = JsonSerializable.Instance;
        
        /// <summary>
        ///   <para>序列化工具</para>
        /// </summary>
        public static ISerializable Serializable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Serializable;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => s_Serializable = value ?? JsonSerializable.Instance;
        }
    }
}