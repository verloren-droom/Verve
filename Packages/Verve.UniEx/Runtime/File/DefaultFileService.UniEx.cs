#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.File
{
    using System.IO;
    using UnityEngine;
    
    
    public sealed partial class DefaultFileService : Verve.File.FileServiceBase 
    {
        public override string PersistentDataPath =>
#if UNITY_EDITOR
            Path.Combine(Application.dataPath, ".Cache");
#else
            Application.persistentDataPath;
#endif
        
        public override string TemporaryPath =>
#if UNITY_EDITOR
            Path.Combine(Application.dataPath, ".Temp");
#else
            Application.temporaryCachePath;
#endif
    }
}

#endif