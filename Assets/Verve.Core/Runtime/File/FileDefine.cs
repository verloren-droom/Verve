namespace Verve.File
{
    
    using System.IO;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif


    /// <summary>
    /// 文件相关的共享变量和方法
    /// </summary>
    public sealed partial class FileDefine
    {
        private static string m_BasePath =
#if UNITY_5_3_OR_NEWER
            Application.persistentDataPath;
#else
#endif
 

        public static string BasePath
        {
            get
            {
#if UNITY_EDITOR
                return Path.Combine(Application.dataPath, ".Cache");
#else
                return m_BasePath;
#endif
            }
            set => m_BasePath = value;
        }
        
        /// <summary>
        /// 临时缓存文件夹路径
        /// </summary>
        public static string TempPath =>
#if UNITY_5_3_OR_NEWER
            Application.temporaryCachePath;
#else
#endif
        
        public static string GetFileAbsolutePath(string relativePath)
        {
            return Path.Combine(BasePath, relativePath);
        }
    }
    
}