namespace Verve.Platform
{
    using System;
    using System.IO;

    
    /// <summary>
    /// 平台子模块基类
    /// </summary>
    public abstract class PlatformSubmodule : IPlatformSubmodule
    {
        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }
        public virtual void OnModuleUnloaded() { }
        
        public virtual void OpenUrl(string url) => System.Diagnostics.Process.Start(url);
        public virtual void ShowDialog(string title, string message) { }
        public virtual void OpenFilePicker(Action<string> onFileSelected, string filter = "All files (*.*)|*.*") { onFileSelected?.Invoke(null); }
        public virtual string GetPersistentDataPath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public virtual string GetTemporaryCachePath() => Path.GetTempPath();
        public virtual void CopyToClipboard(string text) { }
        public virtual string GetClipboardText() => string.Empty;
        
        protected string[] ParseFilter(string filter)
        {
            filter = filter.Trim();
            if (string.IsNullOrEmpty(filter)) 
                return Array.Empty<string>();

            try
            {
                string[] parts = filter.Split('|');
                string[] result = new string[parts.Length];
            
                for (int i = 0; i < parts.Length; i++)
                {
                    result[i] = parts[i].Trim();
                }
            
                return result;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}