#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Runtime.InteropServices;
    
    
    /// <summary>
    ///   <para>Mac平台</para>
    /// </summary>
    internal sealed class MacPlatform : GenericPlatform
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
            
            string safeTitle = string.IsNullOrEmpty(title) ? "Dialog" : title;
            string safeMessage = string.IsNullOrEmpty(message) ? "" : message;
            string safeOkText = string.IsNullOrEmpty(okText) ? "OK" : okText;
            string safeCancelText = string.IsNullOrEmpty(cancelText) ? null : cancelText;
            
            try
            {
                using var titleUtf8 = new Utf8String(safeTitle);
                using var messageUtf8 = new Utf8String(safeMessage);
                using var okUtf8 = new Utf8String(safeOkText);
                using var cancelUtf8 = new Utf8String(safeCancelText);

                if (titleUtf8.Ptr == IntPtr.Zero || messageUtf8.Ptr == IntPtr.Zero || okUtf8.Ptr == IntPtr.Zero)
                {
                    onResult?.Invoke(false);
                    return;
                }
                
                int dialogResult = _ShowDialog(
                    titleUtf8.Ptr,
                    messageUtf8.Ptr,
                    okUtf8.Ptr,
                    cancelUtf8.Ptr
                );
                
                onResult?.Invoke(dialogResult == 0);
            }
            catch
            {
                onResult?.Invoke(false);
            }
        }
    }
}

#endif
