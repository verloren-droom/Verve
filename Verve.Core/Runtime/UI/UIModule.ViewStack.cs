#if UNITY_5_3_OR_NEWER

namespace Verve
{
    /// <summary>
    ///   <para>UI 模块。</para>
    /// </summary>
    sealed partial class UIModule
    {
        /// <summary>
        ///   <para>页面栈。</para>
        /// </summary>
        private ViewStack m_ViewStack;

        /// <summary>
        ///   <para>返回按页面打开顺序维护的可选返回栈。</para>
        /// </summary>
        public ViewStack ViewStack => m_ViewStack;
    }
}

#endif
