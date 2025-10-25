#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    
    
    public static class GamePlatformFileSystemExtension
    {
        /// <summary>
        /// 获取多个文件信息
        /// </summary>
        /// <param name="filePaths">文件路径集合</param>
        /// <returns>文件信息枚举</returns>
        public static IEnumerable<FileInfo> GetFileInfos(this IGamePlatformFileSystem self, IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                yield return self.GetFileInfo(filePath);
            }
        }
        
        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="text">文本</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static bool WriteFile(this IGamePlatformFileSystem self, string filePath, string text, bool overwrite = true, Encoding encoding = null)
        {
            return self.WriteFile(filePath, (encoding ?? Encoding.UTF8).GetBytes(text), overwrite);
        }
        
        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">编码</param>
        public static string ReadFile(this IGamePlatformFileSystem self, string filePath, Encoding encoding = null)
        {
            return encoding?.GetString(self.ReadFile(filePath)) ?? Encoding.UTF8.GetString(self.ReadFile(filePath));
        }
        
        /// <summary>
        /// 获取文件的 MD5 值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static string GetMD5(this IGamePlatformFileSystem self, string filePath)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(self.PersistentDataPath, filePath);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return self.GetMD5(stream);
        }
        
        /// <summary>
        /// 获取文件的 MD5 值
        /// </summary>
        /// <param name="stream">文件流</param>
        public static string GetMD5(this IGamePlatformFileSystem _, Stream stream)
        {
            long originalPosition = stream.CanSeek ? stream.Position : 0;
    
            try
            {
                using var md5 = MD5.Create();
                if (stream.CanSeek) stream.Position = 0;
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
            finally
            {
                if (stream.CanSeek) stream.Position = originalPosition;
            }
        }
        
        /// <summary>
        /// 获取 SHA256 值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static string GetSHA256(this IGamePlatformFileSystem self, string filePath)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(self.PersistentDataPath, filePath);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return self.GetSHA256(stream);
        }
        
        /// <summary>
        /// 获取文件的 SHA256 值
        /// </summary>
        /// <param name="stream">文件流</param>
        public static string GetSHA256(this IGamePlatformFileSystem _, Stream stream)
        {
            long originalPosition = stream.CanSeek ? stream.Position : 0;
            try
            {
                using var sha = SHA256.Create();
                if (stream.CanSeek) stream.Position = 0;
                byte[] hashBytes = sha.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
            finally
            {
                if (stream.CanSeek) stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// 获取 SHA256 值
        /// </summary>
        /// <param name="buffer">文件字节数组</param>
        public static string GetSHA256(this IGamePlatformFileSystem self, byte[] buffer)
        {
            var bytes = buffer;
            // if (bytes.Length >= 3 && 
            //     bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            // {
            //     bytes = bytes[3..];
            // }
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLower();
        }
    }
}

#endif