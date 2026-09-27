namespace Verve
{
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>加解密工具；未选择项目实现时使用 AES。</para>
        /// </summary>
        public static ICrypto Crypto => GetTool<ICrypto>();
    }
}