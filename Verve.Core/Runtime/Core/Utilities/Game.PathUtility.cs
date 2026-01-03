namespace Verve
{
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>路径工具类</para>
        /// </summary>
        public static class PathUtility
        {
            /// <summary>
            ///   <para>获取规范路径</para>
            /// </summary>
            /// <param name="path">路径</param>
            /// <returns>
            ///   <para>规范路径</para>
            /// </returns>
            public static string GetNormalizePath(string path)
            {
                return path?.Replace('\\', '/');
            }
        }
    }
}