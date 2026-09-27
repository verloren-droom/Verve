namespace Verve
{
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>序列化工具；借用当前应用的实现。</para>
        /// </summary>
        public static ISerializer Serializer => GetTool<ISerializer>();
    }
}