namespace Verve.Platform
{
    /// <summary>
    /// 平台接口
    /// </summary>
    public interface IPlatform
    {
        /// <summary> 打开网址 </summary>
        void OpenUrl(string url);

        /// <summary> 显示进度条 </summary>
        void ShowProgressBar(string title, string message, float progress);
        /// <summary> 显示对话框 </summary>
        void ShowDialog(string title, string message, string okText = "确定");
        void ShowDialog(string title, string message, System.Action<bool> onResult, string okText = "确定", string cancelText = "取消");
        
        /// <summary>显示吐司</summary>
        // void ShowToast(string message);
        
        /// <summary> 打开系统文件选择器 </summary>
        void OpenFilePicker(System.Action<string> onFileSelected, string filter = "All files (*.*)|*.*");

        /// <summary> 获取项目路径 </summary>
        string GetProjectPath();

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