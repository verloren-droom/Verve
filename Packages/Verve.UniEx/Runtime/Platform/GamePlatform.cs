namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Security.Policy;
    
    
    /// <summary>
    /// 游戏平台
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
        
        /// <summary> 应用名称（只读） </summary>
        public static string AppName => s_Platform.AppName;
        
        /// <summary> 应用版本（只读） </summary>
        public static string AppVersion => s_Platform.AppVersion;
        
        /// <summary> 运行时平台（只读） </summary>
        public static RuntimePlatform Platform 
        {
            get
            {
                if (Enum.TryParse(s_Platform.PlatformName, out RuntimePlatform platform))
                    return platform;
                throw new ArgumentException($"{s_Platform.PlatformName} is not a valid RuntimePlatform.");
            }
        }
        
        /// <summary> 设备ID（只读） </summary>
        public static string DeviceId => s_Platform.DeviceId;
        
        /// <summary> 电池电量（只读） </summary>
        public static float BatteryLevel => s_Platform.BatteryLevel;
        
        /// <summary> 系统语言（只读） </summary>
        public static SystemLanguage Language => Enum.TryParse(s_Platform.Language, out SystemLanguage platform) ? platform : SystemLanguage.ChineseSimplified;

        /// <summary>
        /// 拷贝文本到剪贴板
        /// </summary>
        /// <param name="text">文本</param>
        public static void CopyToClipboard(string text) => s_Platform.CopyToClipboard(text);
        
        /// <summary>
        /// 获取剪贴板文本
        /// </summary>
        /// <returns>剪贴板文本</returns>
        public static string GetClipboardText() => s_Platform.GetClipboardText();
        
        /// <summary> 项目路径（只读） </summary>
        public static string ProjectPath => s_Platform.ProjectPath;
        
        /// <summary> 持久化数据路径（只读） </summary>
        public static string PersistentDataPath => s_Platform.PersistentDataPath;
        
        /// <summary> 临时缓存路径（只读） </summary>
        public static string TemporaryCachePath => s_Platform.TemporaryCachePath;
        
        /// <summary>
        /// 打开文件选择器
        /// </summary>
        public static void OpenFilePicker(Action<string> onFileSelected, string filter = "All files (*.*)|*.*") => s_Platform.OpenFilePicker(onFileSelected, filter);
        
        /// <summary>
        /// 打开网址
        /// </summary>
        /// <param name="url">网址</param>
        public static void OpenUrl(string url) => s_Platform.OpenUrl(url);
        
        /// <summary>
        /// 打开网址
        /// </summary>
        /// <param name="url">网址</param>
        public static void OpenUrl(Url url) => s_Platform.OpenUrl(url.ToString());
        
        /// <summary>
        /// 显示进度条
        /// </summary>
        public static void ShowProgressBar(string title, string message, float progress) => s_Platform.ShowProgressBar(title, message, progress);
        
        /// <summary> 隐藏进度条 </summary>
        public static void HideProgressBar() => s_Platform.HideProgressBar();
        
        /// <summary>
        /// 显示对话框
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="onResult"></param>
        /// <param name="okText"></param>
        /// <param name="cancelText"></param>
        public static void ShowDialog(string title, string message, Action<bool> onResult, string okText = "确定", string cancelText = "取消") => s_Platform.ShowDialog(title, message, onResult, okText, cancelText);
        
        /// <summary> 退出游戏 </summary>
        public static void Quit() => s_Platform.Quit();
        
        /// <summary> 重启游戏 </summary>
        public static void Restart() => s_Platform.Restart();
        
        /// <summary> 设置保持屏幕常亮 </summary>
        public static void SetKeepScreenOn(bool keepOn) => s_Platform.SetKeepScreenOn(keepOn);
        
        /// <summary>
        /// 震动
        /// </summary>
        /// <param name="milliseconds"></param>
        public static void Vibrate(long milliseconds = 100) => s_Platform.Vibrate(milliseconds);

        /// <summary> 运行时后台运行 </summary>
        public static bool RunInBackground
        {
            get => s_Platform.RunInBackground;
            set => s_Platform.RunInBackground = value;
        }
        
        /// <summary>
        /// 应用程序获取或失去焦点时调用
        /// </summary>
        public static event Action<bool> OnApplicationFocus
        {
            add => s_Platform.OnApplicationFocus += value;
            remove => s_Platform.OnApplicationFocus -= value;
        }

        /// <summary>
        /// 退出时调用
        /// </summary>
        public static event Action OnApplicationQuit
        {
            add => s_Platform.OnApplicationQuit += value;
            remove => s_Platform.OnApplicationQuit -= value;
        }
        
        /// <summary>
        /// 运行时内存不足时调用
        /// </summary>
        public static event Action OnLowMemory
        {
            add => s_Platform.OnLowMemory += value;
            remove => s_Platform.OnLowMemory -= value;
        }
    }
}