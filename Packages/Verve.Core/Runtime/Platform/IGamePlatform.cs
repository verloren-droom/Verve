namespace Verve
{
    /// <summary>
    ///   <para>游戏平台接口</para>
    /// </summary>
    public interface IGamePlatform : IGamePlatformEvents
    {
        /// <summary>
        ///   <para>应用程序名称</para>
        /// </summary>
        string AppName { get; }
        
        /// <summary>
        ///   <para>应用程序版本</para>
        /// </summary>
        string AppVersion { get; }
        
        /// <summary>
        ///   <para>平台名称</para>
        /// </summary>
        string PlatformName { get; }
        
        /// <summary>
        ///   <para>系统语言</para>
        /// </summary>
        string Language { get; }
        
        /// <summary>
        ///  <para>运行时是否后台运行</para>
        /// </summary>
        bool RunInBackground { get; set; }

        /// <summary>
        ///   <para>退出应用程序</para>
        /// </summary>
        void Quit();
        
        /// <summary>
        ///   <para>重启应用程序</para>
        /// </summary>
        void Restart();
        
        /// <summary>
        ///   <para>设备唯一标识</para>
        /// </summary>
        string DeviceId { get; }
        
        /// <summary>
        ///   <para>设备电量</para>
        /// </summary>
        float BatteryLevel { get; }
        
        /// <summary>
        ///   <para>文件系统</para>
        /// </summary>
        IGamePlatformFileSystem FileSystem { get; }
        
        /// <summary>
        ///   <para>打开网址</para>
        /// </summary>
        /// <param name="url">网址</param>
        void OpenUrl(string url);
        
        /// <summary>
        ///   <para>打开系统文件选择器</para>
        /// </summary>
        /// <param name="onFileSelected">文件选择完成回调</param>
        /// <param name="filter">文件过滤器</param>
        void OpenFilePicker(System.Action<string> onFileSelected, string filter = "All files (*.*)|*.*");
        
        /// <summary>
        ///   <para>复制文本到剪贴板</para>
        /// </summary>
        /// <param name="text">文本</param>
        void CopyToClipboard(string text);
        
        /// <summary>
        ///   <para>从剪贴板获取文本</para>
        /// </summary>
        /// <returns>
        ///   <para>剪贴板文本</para>
        /// </returns>
        string GetClipboardText();

        /// <summary>
        ///   <para>震动</para>
        /// </summary>
        /// <param name="milliseconds">震动时长</param>
        void Vibrate(long milliseconds = 100);
        
        /// <summary>
        ///   <para>设置屏幕常亮</para>
        /// </summary>
        /// <param name="keepOn">是否常亮</param>
        void SetKeepScreenOn(bool keepOn);
        
        /// <summary>
        ///   <para>显示对话框</para>
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">内容</param>
        /// <param name="onResult">结果回调</param>
        /// <param name="okText">确定按钮文本</param>
        /// <param name="cancelText">取消按钮文本</param>
        void ShowDialog(string title, string message, System.Action<bool> onResult, string okText = "确定", string cancelText = "取消");

        /// <summary>
        ///   <para>显示加载框</para>
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">内容</param>
        /// <param name="progress">进度</param>
        void ShowProgressBar(string title, string message, float progress);
        
        /// <summary>
        ///   <para>隐藏加载框</para>
        /// </summary>
        void HideProgressBar();
    }
}