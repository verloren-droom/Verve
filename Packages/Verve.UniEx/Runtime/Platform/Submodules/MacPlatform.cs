#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

namespace Verve.UniEx.Platform
{
    using System;
    using UnityEngine;
    using System.Runtime.InteropServices;
    
    
    /// <summary>
    /// Mac平台
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(PlatformGameFeature), Description = "Mac平台")]
    public sealed class MacPlatform : GenericPlatform
    {
        private delegate void DialogCallback(bool result);
        private static DialogCallback s_currentCallback;
        
        [DllImport("__Internal", EntryPoint = "_ShowDialog")]
        private static extern void _ShowDialog(
            IntPtr title,
            IntPtr message,
            IntPtr okText,
            IntPtr cancelText,
            IntPtr callback);
    
        public static void ShowDialog(string title, string message, string okText = "确定")
        {
            ShowDialog(title, message, null, okText, null);
        }
    
        public static void ShowDialog(string title, string message, Action<bool> onResult, 
            string okText = "确定", string cancelText = "取消")
        {
            s_currentCallback = new DialogCallback((result) => {
                onResult?.Invoke(result);
                s_currentCallback = null;
            });

            using var titlePtr = new Utf8StringPtr(title);
            using var messagePtr = new Utf8StringPtr(message);
            using var okTextPtr = new Utf8StringPtr(okText);
            using var cancelTextPtr = new Utf8StringPtr(cancelText);
            try
            {
                var callbackPtr = Marshal.GetFunctionPointerForDelegate(s_currentCallback);
                _ShowDialog(
                    titlePtr,
                    messagePtr,
                    okTextPtr,
                    string.IsNullOrEmpty(cancelText) ? IntPtr.Zero : cancelTextPtr,
                    callbackPtr
                );
            }
            catch
            {
                onResult?.Invoke(false);
                s_currentCallback = null;
            }
        }
    }
    
    
    internal sealed class Utf8StringPtr : IDisposable
    {
        private readonly IntPtr m_Ptr;
        private readonly int m_Length;
    
        internal Utf8StringPtr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                m_Ptr = IntPtr.Zero;
                m_Length = 0;
                return;
            }
        
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            m_Length = bytes.Length;
            m_Ptr = Marshal.AllocHGlobal(m_Length + 1);
            Marshal.Copy(bytes, 0, m_Ptr, m_Length);
            Marshal.WriteByte(m_Ptr, m_Length, 0);
        }
    
        public static implicit operator IntPtr(Utf8StringPtr ptr) => ptr.m_Ptr;
    
        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_Ptr);
            }
        }
    }
}

#endif