namespace Verve
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;

    /// <summary>
    ///   <para>非托管字符串句柄；持有供 P/Invoke 使用的 UTF-8 内存。</para>
    /// </summary>
    public sealed class Utf8String : DisposableObject
    {
        /// <summary>
        ///   <para>非托管内存指针。</para>
        /// </summary>
        private IntPtr m_Ptr;
        /// <summary>
        ///   <para>非托管内存指针（以 0 结尾）。</para>
        /// </summary>
        public IntPtr Ptr => m_Ptr;
        /// <summary>
        ///   <para>是否为空指针（表示已经释放）。</para>
        /// </summary>
        public bool IsNull => m_Ptr == IntPtr.Zero;

        /// <summary>
        ///   <para>创建 UTF-8 字符串句柄。</para>
        /// </summary>
        /// <param name="str">文本。</param>
        public Utf8String(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (str.IndexOf('\0') >= 0) throw new ArgumentException("Native strings cannot contain embedded null characters.", nameof(str));
            m_Ptr = Marshal.StringToCoTaskMemUTF8(str);
        }

        /// <summary>
        ///   <para>回收未释放的资源。</para>
        /// </summary>
        ~Utf8String() => Release();

        protected override void OnDispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   <para>释放非托管内存。</para>
        /// </summary>
        private void Release()
        {
            var ptr = Interlocked.Exchange(ref m_Ptr, IntPtr.Zero);
            if (ptr == IntPtr.Zero) return;
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}