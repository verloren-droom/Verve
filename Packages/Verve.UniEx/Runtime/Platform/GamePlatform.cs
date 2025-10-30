#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Security.Policy;
    
    
    /// <summary>
    ///   <para>游戏平台</para>
    ///   <para>封装了平台相关的功能</para>
    /// </summary>
    [Serializable]
    public sealed class GamePlatform
    {
        private static IGamePlatform s_Platform = CreateDefaultPlatform();

        private static IGamePlatform CreateDefaultPlatform()
        {
#if UNITY_EDITOR
            return new GenericPlatform();
#else
            if (Platform == RuntimePlatform.OSXPlayer)
            {
                return new MacPlatform();
            }
            else
            {
                return new GenericPlatform();
            }
#endif
        }
        
        /// <summary>
        ///   <para>应用名称（只读）</para>
        /// </summary>
        public static string AppName => s_Platform.AppName;
        
        /// <summary>
        ///   <para>应用版本（只读）</para>
        /// </summary>
        public static string AppVersion => s_Platform.AppVersion;
        
        /// <summary>
        ///   <para>运行时平台（只读）</para>
        /// </summary>
        public static RuntimePlatform Platform 
        {
            get
            {
                if (Enum.TryParse(s_Platform.PlatformName, out RuntimePlatform platform))
                    return platform;
                throw new ArgumentException($"{s_Platform.PlatformName} is not a valid RuntimePlatform.");
            }
        }
        
        /// <summary>
        ///   <para>设备ID（只读）</para>
        /// </summary>
        public static string DeviceId => s_Platform.DeviceId;
        
        /// <summary>
        ///   <para>电池电量（只读）</para>
        /// </summary>
        public static float BatteryLevel => s_Platform.BatteryLevel;
        
        /// <summary>
        ///   <para>系统语言（只读）</para>
        /// </summary>
        public static SystemLanguage Language => Enum.TryParse(s_Platform.Language, out SystemLanguage platform) ? platform : SystemLanguage.ChineseSimplified;

        /// <summary>
        ///   <para>拷贝文本到剪贴板</para>
        /// </summary>
        /// <param name="text">文本</param>
        public static void CopyToClipboard(string text) => s_Platform.CopyToClipboard(text);
        
        /// <summary>
        ///   <para>获取剪贴板文本</para>
        /// </summary>
        /// <returns>
        ///   <para>剪贴板文本</para>
        /// </returns>
        public static string GetClipboardText() => s_Platform.GetClipboardText();
        
        /// <summary>
        ///   <para>文件系统</para>
        /// </summary>
        public static IGamePlatformFileSystem FileSystem => s_Platform.FileSystem;
        
        /// <summary>
        ///   <para>打开文件选择器</para>
        /// </summary>
        /// <param name="onFileSelected">文件选择回调</param>
        /// <param name="filter">文件过滤器</param>
        public static void OpenFilePicker(Action<string> onFileSelected, string filter = "All files (*.*)|*.*") => s_Platform.OpenFilePicker(onFileSelected, filter);
        
        /// <summary>
        ///   <para>打开网址</para>
        /// </summary>
        /// <param name="url">网址</param>
        public static void OpenUrl(string url) => s_Platform.OpenUrl(url);
        
        /// <summary>
        ///   <para>打开网址</para>
        /// </summary>
        /// <param name="url">网址</param>
        public static void OpenUrl(Url url) => s_Platform.OpenUrl(url.ToString());
        
        /// <summary>
        ///   <para>显示进度条</para>
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="progress">进度</param>
        public static void ShowProgressBar(string title, string message, float progress) => s_Platform.ShowProgressBar(title, message, progress);
        
        /// <summary>
        ///   <para>隐藏进度条</para>
        /// </summary>
        public static void HideProgressBar() => s_Platform.HideProgressBar();
        
        /// <summary>
        ///   <para>显示对话框</para>
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="onResult">结果回调</param>
        /// <param name="okText">确定按钮文本</param>
        /// <param name="cancelText">取消按钮文本</param>
        public static void ShowDialog(string title, string message, Action<bool> onResult, string okText = "确定", string cancelText = "取消") => s_Platform.ShowDialog(title, message, onResult, okText, cancelText);
        
        /// <summary>
        ///   <para>退出游戏</para>
        /// </summary>
        public static void Quit() => s_Platform.Quit();
        
        /// <summary>
        ///   <para>重启游戏</para>
        /// </summary>
        public static void Restart() => s_Platform.Restart();
        
        /// <summary>
        ///   <para>设置保持屏幕常亮</para>
        /// </summary>
        public static void SetKeepScreenOn(bool keepOn) => s_Platform.SetKeepScreenOn(keepOn);
        
        /// <summary>
        ///   <para>震动</para>
        /// </summary>
        /// <param name="milliseconds">震动时长</param>
        public static void Vibrate(long milliseconds = 100) => s_Platform.Vibrate(milliseconds);

        /// <summary>
        ///   <para>运行时后台运行</para>
        /// </summary>
        public static bool RunInBackground
        {
            get => s_Platform.RunInBackground;
            set => s_Platform.RunInBackground = value;
        }
        
        /// <summary>
        ///   <para>应用程序获取或失去焦点时调用</para>
        /// </summary>
        public static event Action<bool> OnApplicationFocus
        {
            add => s_Platform.OnApplicationFocus += value;
            remove => s_Platform.OnApplicationFocus -= value;
        }

        /// <summary>
        ///   <para>退出时调用</para>
        /// </summary>
        public static event Action OnApplicationQuit
        {
            add => s_Platform.OnApplicationQuit += value;
            remove => s_Platform.OnApplicationQuit -= value;
        }
        
        /// <summary>
        ///   <para>运行时内存不足时调用</para>
        /// </summary>
        public static event Action OnLowMemory
        {
            add => s_Platform.OnLowMemory += value;
            remove => s_Platform.OnLowMemory -= value;
        }
    }
}

#endif