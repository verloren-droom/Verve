namespace Verve.File
{
    using System;
    using System.Security.Cryptography;
    

    /// <summary>
    /// 文件扩展
    /// </summary>
    public static class FileExtension
    {
        /// <summary>
        /// 获取文件的 MD5 值
        /// </summary>
        public static string GetMD5(this IFileSystem _, System.IO.Stream stream)
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
        /// 获取文件的 SHA256 值
        /// </summary>
        public static string GetSHA256(this IFileSystem _, System.IO.Stream stream)
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
        public static string GetSHA256(this IFileSystem _, byte[] buffer)
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