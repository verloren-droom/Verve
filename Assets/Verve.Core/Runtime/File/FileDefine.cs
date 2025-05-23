namespace Verve.File
{
    using System;
    using System.IO;


    /// <summary>
    /// 文件相关的共享变量和方法
    /// </summary>
    public static partial class FileDefine
    {
        public const string JSON_EXTENSION = ".json";
        public const string TEXT_EXTENSION = ".txt";
        public const string BINARY_EXTENSION = ".bin";
        
        
        /// <summary>
        /// 持久化资源路径
        /// </summary>
        public static string PersistentDataPath =>
#if UNITY_EDITOR
            Path.Combine(UnityEngine.Application.dataPath, ".Cache");
#elif UNITY_5_3_OR_NEWER
            UnityEngine.Application.persistentDataPath;
#else
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif

        /// <summary>
        /// 临时缓存文件夹路径
        /// </summary>
        public static string TempPath =>
#if UNITY_EDITOR
            Path.Combine(UnityEngine.Application.dataPath, ".Temp");
#elif UNITY_5_3_OR_NEWER
            UnityEngine.Application.temporaryCachePath;
#else
            Path.GetTempPath();
#endif
        
        /// <summary>
        /// 临时缓存文件路径
        /// </summary>
        public static string TempFilePath => Path.Combine(TempPath, TempFileName);

        /// <summary>
        /// 临时缓存文件名
        /// </summary>
        public static string TempFileName =>
#if UNITY_5_3_OR_NEWER
            Guid.NewGuid().ToString();
#else
            Path.GetTempFileName();
#endif
        
        /// <summary>
        /// 获取持久化文件路径
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static string GetPersistentFilePath(string relativePath) => 
            Path.Combine(PersistentDataPath, relativePath);
    }
    
}