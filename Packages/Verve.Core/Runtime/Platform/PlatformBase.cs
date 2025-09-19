namespace Verve.Platform
{
    using System;
    using System.IO;

    
    /// <summary>
    /// 平台基类
    /// </summary>
    public abstract class PlatformBase : IPlatform
    {
        public virtual void OpenUrl(string url) => System.Diagnostics.Process.Start(url);
        public virtual void ShowProgressBar(string title, string message, float progress) => Console.WriteLine($"[{Math.Round(progress * 100, 1)}%] {title}: {message}");
        public virtual void ShowDialog(string title, string message, Action<bool> onResult = null, string okText = "确定", string cancelText = "取消")
        {
            Console.WriteLine($"{title}: {message}");
            Console.WriteLine($"[Y] {okText}   [N] {cancelText}");
            
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
            } while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N);
            
            Console.WriteLine();
            onResult?.Invoke(key.Key == ConsoleKey.Y);
        }
        public virtual void ShowDialog(string title, string message, string okText = "确定")
        {
            Console.WriteLine($"{title}: {message}");
            Console.WriteLine($"[{okText}]");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        public virtual void OpenFilePicker(Action<string> onFileSelected, string filter = "All files (*.*)|*.*") { onFileSelected?.Invoke(null); }
        public virtual string GetPersistentDataPath() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public virtual string GetTemporaryCachePath() => Path.GetTempPath();

        public virtual string GetProjectPath()
        {
            try
            {
                var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    return Path.GetDirectoryName(entryAssembly.Location);
                }
                
                var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                if (executingAssembly != null)
                {
                    return Path.GetDirectoryName(executingAssembly.Location);
                }
                
                return Directory.GetCurrentDirectory();
            }
            catch
            {
                return Directory.GetCurrentDirectory();
            }
        }
        
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