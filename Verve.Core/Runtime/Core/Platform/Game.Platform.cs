namespace Verve
{
    /// <summary>
    ///   <para>游戏入口：平台适配部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>游戏平台</para>
        /// </summary>
        public static readonly IGamePlatform platform = 
#if UNITY_EDITOR
        new GenericPlatform();
#elif UNITY_STANDALONE_OSX
        new MacPlatform();
#else
        new GenericPlatform();
#endif
    }
}