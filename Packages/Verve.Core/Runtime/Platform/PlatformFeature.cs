namespace Verve.Platform
{
    /// <summary>
    /// 平台功能
    /// </summary>
    [System.Serializable]
    public class PlatformFeature : GameFeature
    {
        protected IPlatformSubmodule m_Platform;
        
        
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            
            m_Platform = new GenericPlatformSubmodule();
            m_Platform.OnModuleLoaded(dependencies);
        }
        
        protected override void OnUnload()
        {
            base.OnUnload();
            
            m_Platform?.OnModuleUnloaded();
            m_Platform = null;
        }

        public string GetPersistentDataPath() => m_Platform?.GetPersistentDataPath();
        public string GetTemporaryCachePath() => m_Platform?.GetTemporaryCachePath();
        
        public void CopyToClipboard(string text) => m_Platform?.CopyToClipboard(text);
        public string GetClipboardText() => m_Platform?.GetClipboardText();
        public void OpenFilePicker(System.Action<string> onFileSelected, string filter = "All files (*.*)|*.*") => m_Platform?.OpenFilePicker(onFileSelected, filter);
        public void OpenUrl(string url) => m_Platform?.OpenUrl(url);
        public void ShowDialog(string title, string message) => m_Platform?.ShowDialog(title, message);
    }
}