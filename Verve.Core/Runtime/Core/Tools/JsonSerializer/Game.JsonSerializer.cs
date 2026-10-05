namespace Verve
{
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>JSON 序列化工具。</para>
        /// </summary>
        public static IJsonSerializer JsonSerializer => GetTool<IJsonSerializer>();
    }
}