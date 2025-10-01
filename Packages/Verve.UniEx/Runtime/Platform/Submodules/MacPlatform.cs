#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

namespace Verve.UniEx.Platform
{
    using System;
    using UnityEngine;
    using System.Text;
    using System.Runtime.InteropServices;
    
    
    /// <summary>
    /// Mac平台
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(PlatformGameFeature), Description = "Mac平台")]
    public sealed class MacPlatform : GenericPlatform
    {
        [DllImport("__Internal")]
        private static extern int _ShowDialog(
            IntPtr title,
            IntPtr message,
            IntPtr defaultButton,
            IntPtr alternateButton);
        
        [DllImport("__Internal")]
        private static extern int _ShowDialog(
            string title,
            string message,
            string defaultButton,
            string alternateButton);

        public override void ShowDialog(string title, string message, string okText = "确定")
        {
            ShowDialog(title, message, null, okText, null);
        }

        public override void ShowDialog(string title, string message, Action<bool> onResult = null, 
            string okText = "确定", string cancelText = "取消")
        {
            if (!Application.isPlaying) 
            {
                onResult?.Invoke(false);
                return;
            }
            
            IntPtr titlePtr = IntPtr.Zero;
            IntPtr messagePtr = IntPtr.Zero;
            IntPtr okTextPtr = IntPtr.Zero;
            IntPtr cancelTextPtr = IntPtr.Zero;
            
            string safeTitle = string.IsNullOrEmpty(title) ? "Dialog" : title;
            string safeMessage = string.IsNullOrEmpty(message) ? "" : message;
            string safeOkText = string.IsNullOrEmpty(okText) ? "OK" : okText;
            string safeCancelText = string.IsNullOrEmpty(cancelText) ? null : cancelText;
            
            try
            {
                titlePtr = StringToUtf8Ptr(safeTitle);
                messagePtr = StringToUtf8Ptr(safeMessage);
                okTextPtr = StringToUtf8Ptr(safeOkText);
                cancelTextPtr = StringToUtf8Ptr(safeCancelText);
                
                if (titlePtr == IntPtr.Zero || messagePtr == IntPtr.Zero || okTextPtr == IntPtr.Zero)
                {
                    onResult?.Invoke(false);
                    return;
                }
                
                int dialogResult = _ShowDialog(
                    titlePtr,
                    messagePtr,
                    okTextPtr,
                    cancelTextPtr
                );
                
                onResult?.Invoke(dialogResult == 0);
            }
            catch
            {
                onResult?.Invoke(false);
            }
            finally
            {
                FreeUtf8Ptr(titlePtr);
                FreeUtf8Ptr(messagePtr);
                FreeUtf8Ptr(okTextPtr);
                FreeUtf8Ptr(cancelTextPtr);
            }
        }
        
        /// <summary>
        /// 将字符串转换为UTF8编码的非托管内存指针
        /// </summary>
        private static IntPtr StringToUtf8Ptr(string str)
        {
            if (string.IsNullOrEmpty(str))
                return IntPtr.Zero;
                
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                Marshal.WriteByte(ptr, bytes.Length, 0); // 添加null终止符
                
                string roundTrip = Marshal.PtrToStringUTF8(ptr);
                if (roundTrip != str)
                {
                    Debug.LogWarning($"[MacPlatform] String round-trip mismatch: '{str}' -> '{roundTrip}'");
                }
                
                return ptr;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacPlatform] Failed to convert string '{str}' to pointer: {ex}");
                return IntPtr.Zero;
            }
        }
        
        /// <summary>
        /// 释放UTF8字符串指针
        /// </summary>
        private static void FreeUtf8Ptr(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                try
                {
                    Marshal.FreeHGlobal(ptr);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MacPlatform] Error freeing memory at {ptr}: {ex}");
                }
            }
        }
    }
}

#endif