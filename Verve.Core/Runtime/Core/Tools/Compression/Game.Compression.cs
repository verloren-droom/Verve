namespace Verve
{
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>压缩工具。</para>
        /// </summary>
        public static ICompression Compression => GetTool<ICompression>();
    }
}