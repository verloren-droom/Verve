namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        [ThreadStatic] private static ICompression s_Compression = GZipCompression.Instance;
        
        /// <summary>
        ///   <para>压缩工具</para>
        /// </summary>
        public static ICompression Compression
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Compression;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => s_Compression = value ?? GZipCompression.Instance;
        }
    }
}