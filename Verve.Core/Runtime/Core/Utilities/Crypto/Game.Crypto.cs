namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        [ThreadStatic] private static ICrypto s_Crypto = AESCrypto.Instance;

        /// <summary>
        ///   <para>加密工具</para>
        /// </summary>
        public static ICrypto Crypto
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Crypto ??= AESCrypto.Instance;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => s_Crypto = value ?? AESCrypto.Instance;
        }
    }
}