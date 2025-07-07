namespace Verve.Application
{
    using System.Reflection;
    using System.Diagnostics;

    
    /// <summary>
    /// 应用子模块基类
    /// </summary>
    public abstract class ApplicationSubmodule : IApplicationSubmodule
    {
        public virtual string ModuleName { get; }
        public virtual void OnModuleLoaded() { }
        public virtual void OnModuleUnloaded() { }

        public virtual string PlatformName { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

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