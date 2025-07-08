#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    using System.IO;
    using System.Reflection;
    using System.Diagnostics;

    
    /// <summary>
    /// MacOS应用子模块
    /// </summary>
    [System.Serializable]
    public class MacApplicationSubmodule : GenericApplicationSubmodule
    {
        public override string ModuleName => "MacApplication";

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