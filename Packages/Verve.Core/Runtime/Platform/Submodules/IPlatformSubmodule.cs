namespace Verve.Platform
{
    /// <summary>
    /// 平台子模块
    /// </summary>
    public interface IPlatformSubmodule : IGameFeatureSubmodule
    {
        /// <summary> 平台名称 </summary>
        string PlatformName { get; }

        /// <summary> 打开网址 </summary>
        void OpenUrl(string url);
        
        /// <summary> 显示对话框 </summary>
        void ShowDialog(string title, string message);
        
        /// <summary> 打开系统文件选择器 </summary>
        void OpenFilePicker(System.Action<string> onFileSelected, string filter = "All files (*.*)|*.*");

        /// <summary> 获取持久化数据路径 </summary>
        string GetPersistentDataPath();
        
        /// <summary> 获取临时缓存路径 </summary>
        string GetTemporaryCachePath();
        
        /// <summary> 复制文本到剪贴板 </summary>
        void CopyToClipboard(string text);
        
        /// <summary> 从剪贴板获取文本 </summary>
        string GetClipboardText();
        
        /// <summary> 设置屏幕方向 </summary>
        // void ScreenOrientation();
    }
}