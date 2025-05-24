using Verve.File;

#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    public class FileUnit : Verve.File.FileUnit
    {
        protected override IFileService FileService => GetService<DefaultFileService>();


        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            AddService(new DefaultFileService());
        }
    }
}

#endif