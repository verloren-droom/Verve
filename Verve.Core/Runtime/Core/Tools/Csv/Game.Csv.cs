namespace Verve
{
    /// <summary>
    ///   <para>游戏入口；CSV 工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>CSV 工具。</para>
        /// </summary>
        public static ICsv Csv => GetTool<ICsv>();
    }
}