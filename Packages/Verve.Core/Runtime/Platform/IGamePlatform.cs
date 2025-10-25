namespace Verve
{
    /// <summary>
    /// 游戏平台接口
    /// </summary>
    public interface IGamePlatform : IGamePlatformEvents
    {
        /// <summary> 应用程序名称 </summary>
        string AppName { get; }
        
        /// <summary> 应用版本号 </summary>
        string AppVersion { get; }
        
        /// <summary> 平台名称 </summary>
        string PlatformName { get; }
        
        /// <summary> 系统语言 </summary>
        string Language { get; }
        
        /// <summary> 是否后台运行 </summary>
        bool RunInBackground { get; set; }

        /// <summary> 退出应用程序 </summary>
        void Quit();
        
        /// <summary> 重启应用程序 </summary>
        void Restart();
        
        /// <summary> 设备唯一标识 </summary>
        string DeviceId { get; }
        
        /// <summary> 设备电量 </summary>
        float BatteryLevel { get; }
        
        IGamePlatformFileSystem FileSystem { get; }
        
        /// <summary> 打开网址 </summary>
        void OpenUrl(string url);
        
        /// <summary>
        ///  <para>打开系统文件选择器</para>
        /// </summary>
        void OpenFilePicker(System.Action<string> onFileSelected, string filter = "All files (*.*)|*.*");
        
        /// <summary> 复制文本到剪贴板 </summary>
        void CopyToClipboard(string text);
        
        /// <summary> 从剪贴板获取文本 </summary>
        string GetClipboardText();

        /// <summary> 震动 </summary>
        void Vibrate(long milliseconds = 100);
        
        /// <summary> 设置屏幕常亮 </summary>
        void SetKeepScreenOn(bool keepOn);
        
        /// <summary> 显示对话框 </summary>
        void ShowDialog(string title, string message, System.Action<bool> onResult, string okText = "确定", string cancelText = "取消");

        /// <summary> 显示加载框 </summary>
        void ShowProgressBar(string title, string message, float progress);
        
        /// <summary> 隐藏加载框 </summary>
        void HideProgressBar();
    }
}