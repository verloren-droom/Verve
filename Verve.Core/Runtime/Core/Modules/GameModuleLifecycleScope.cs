namespace Verve
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>模块生命周期回调执行范围。</para>
    /// </summary>
    internal static class GameModuleLifecycleScope
    {
        /// <summary>
        ///   <para>生命周期回调嵌套深度。</para>
        /// </summary>
        private static readonly AsyncLocal<int> s_Depth = new();

        /// <summary>
        ///   <para>当前异步调用链是否正在执行模块生命周期回调。</para>
        /// </summary>
        internal static bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_Depth.Value != 0;
        }

        /// <summary>
        ///   <para>进入生命周期回调执行范围。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Scope Enter()
        {
            s_Depth.Value = s_Depth.Value + 1;
            return default;
        }

        /// <summary>
        ///   <para>生命周期回调句柄；释放时退出当前范围。</para>
        /// </summary>
        internal readonly struct Scope : IDisposable
        {
            /// <inheritdoc />
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                int depth = s_Depth.Value;
                s_Depth.Value = depth > 0 ? depth - 1 : 0;
            }
        }
    }
}