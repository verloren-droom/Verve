namespace Verve.Application
{
    using System.Reflection;
    using System.Diagnostics;

    
    /// <summary>
    /// 应用程序基类
    /// </summary>
    public abstract class ApplicationBase : IApplication
    {
        public virtual string Version
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                return assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
                       ?? assembly?.GetName().Version?.ToString() 
                       ?? "1.0.0";
            }
        }

        public virtual string DeviceId => System.Guid.NewGuid().ToString("N").Substring(0, 16);
       
        public virtual void QuitApplication() => System.Environment.Exit(0);

        public virtual void RestartApplication()
        {
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            QuitApplication();
        }
    }
}