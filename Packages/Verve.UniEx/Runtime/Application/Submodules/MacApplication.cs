#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Application
{
    using System.IO;
    using System.Reflection;
    using System.Diagnostics;

    
    /// <summary>
    /// MacOS应用子模块
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(ApplicationGameFeature), Description = "MacOS应用子模块")]
    public class MacApplication : GenericApplication
    {
        public override void RestartApplication()
        {
            string appPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "..", "..");
            if (!string.IsNullOrEmpty(appPath))
            {
                Process.Start(new ProcessStartInfo 
                {
                    FileName = appPath,
                    UseShellExecute = true
                });
                QuitApplication();
            }
        }
    }
}

#endif