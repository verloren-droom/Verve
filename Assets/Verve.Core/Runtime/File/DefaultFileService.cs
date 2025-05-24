namespace Verve.File
{
    using System;
    using System.IO;
    

    public sealed partial class DefaultFileService : FileServiceBase
    {
        public override string PersistentDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        public override string TemporaryPath => Path.GetTempPath();
    }
}