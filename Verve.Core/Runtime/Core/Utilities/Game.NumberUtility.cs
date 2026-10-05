namespace Verve
{
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>数字处理工具。</para>
        /// </summary>
        public static class NumberUtility
        {
            /// <summary>
            ///   <para>判断单精度浮点数是否为有限值。</para>
            /// </summary>
            /// <param name="value">待检查的值。</param>
            public static bool IsFinite(float value)
                => !float.IsNaN(value) && !float.IsInfinity(value);

            /// <summary>
            ///   <para>判断双精度浮点数是否为有限值。</para>
            /// </summary>
            /// <param name="value">待检查的值。</param>
            public static bool IsFinite(double value)
                => !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}