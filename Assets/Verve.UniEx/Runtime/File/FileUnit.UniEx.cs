#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    using Verve.Unit;
    using Verve.File;
    using Verve.Serializable;
    
    
    [CustomUnit("File", dependencyUnits: typeof(SerializableUnit)), System.Serializable]
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