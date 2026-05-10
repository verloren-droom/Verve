namespace Verve
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;
    
    
    /// <summary>
    ///   <para>非托管字符串句柄</para>
    /// </summary>
    /// <remarks>
    ///   <para>该类型用于 P/Invoke 场景，将托管字符串编码后拷贝到非托管堆内存</para>
    /// </remarks>
    public sealed class Utf8String : IDisposable
    {
        private IntPtr m_Ptr;

        /// <summary>
        ///   <para>非托管内存指针（以 0 结尾）</para>
        /// </summary>
        public IntPtr Ptr => m_Ptr;

        /// <summary>
        ///   <para>是否为空指针（通常表示输入为 null 或已经释放）</para>
        /// </summary>
        public bool IsNull => m_Ptr == IntPtr.Zero;

        public Utf8String(string str) : this(str, Encoding.UTF8) { }

        public Utf8String(string str, Encoding encoding)
        {
            if (str == null) return;

            encoding ??= Encoding.UTF8;
            var bytes = encoding.GetBytes(str);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            m_Ptr = ptr;
        }

        ~Utf8String() => Release();

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        private void Release()
        {
            var ptr = Interlocked.Exchange(ref m_Ptr, IntPtr.Zero);
            if (ptr == IntPtr.Zero) return;
            Marshal.FreeHGlobal(ptr);
        }
    }
}