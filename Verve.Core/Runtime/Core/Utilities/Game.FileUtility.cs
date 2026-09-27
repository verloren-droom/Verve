namespace Verve
{
    using System;
    using System.IO;

    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>文件工具类。</para>
        /// </summary>
        public static class FileUtility
        {
            /// <summary>
            ///   <para>每 KB 的字节数。</para>
            /// </summary>
            private const long BytesPerKilobyte = 1024L;
            /// <summary>
            ///   <para>每 MB 的字节数。</para>
            /// </summary>
            private const long BytesPerMegabyte = BytesPerKilobyte * 1024L;
            /// <summary>
            ///   <para>每 GB 的字节数。</para>
            /// </summary>
            private const long BytesPerGigabyte = BytesPerMegabyte * 1024L;
            /// <summary>
            ///   <para>每 TB 的字节数。</para>
            /// </summary>
            private const long BytesPerTerabyte = BytesPerGigabyte * 1024L;

            /// <summary>
            ///   <inheritdoc cref="WriteAllTextAtomically(string,string)"/>
            /// </summary>
            /// <param name="path">路径。</param>
            /// <param name="contents">内容。</param>
            /// <param name="encoding">编码。</param>
            public static bool WriteAllTextAtomically(string path, string contents, System.Text.Encoding encoding)
            {
                var targetPath = Path.GetFullPath(path);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                if (File.Exists(targetPath) && File.ReadAllText(targetPath, encoding) == contents) return false;
                var temporaryPath = GetTemporaryFilePath(targetPath);
                try
                {
                    File.WriteAllText(temporaryPath, contents, encoding);
                    ReplaceFile(temporaryPath, targetPath);
                    return true;
                }
                finally { File.Delete(temporaryPath); }
            }
            
            /// <summary>
            ///   <para>内容变化时通过同目录临时文件原子写入；失败保留原文件。</para>
            /// </summary>
            /// <param name="path">路径。</param>
            /// <param name="contents">内容。</param>
            public static bool WriteAllTextAtomically(string path, string contents)
            {
                return WriteAllTextAtomically(path, contents, System.Text.Encoding.UTF8);
            }

            /// <summary>
            ///   <para>将字节数格式化为易读的文件大小文本。</para>
            /// </summary>
            /// <param name="sizeInBytes">文件大小，单位为字节。</param>
            /// <returns>
            ///   <para>带单位的文件大小文本。</para>
            /// </returns>
            public static string FormatFileSize(long sizeInBytes)
            {
                if (sizeInBytes < BytesPerKilobyte)
                {
                    return sizeInBytes + " B";
                }

                if (sizeInBytes < BytesPerMegabyte)
                {
                    return (sizeInBytes / (double)BytesPerKilobyte).ToString("0.##") + " KB";
                }

                if (sizeInBytes < BytesPerGigabyte)
                {
                    return (sizeInBytes / (double)BytesPerMegabyte).ToString("0.##") + " MB";
                }

                if (sizeInBytes < BytesPerTerabyte)
                {
                    return (sizeInBytes / (double)BytesPerGigabyte).ToString("0.##") + " GB";
                }

                return (sizeInBytes / (double)BytesPerTerabyte).ToString("0.##") + " TB";
            }

            /// <summary>
            ///   <para>为目标文件创建位于同一目录的唯一临时文件路径。</para>
            /// </summary>
            /// <remarks>
            ///   <para>同目录临时文件可用于 <see cref="File.Replace(string, string, string)"/>，避免跨卷替换失败。</para>
            /// </remarks>
            /// <param name="targetFilePath">最终目标文件路径。</param>
            /// <returns>尚未创建的临时文件路径。</returns>
            public static string GetTemporaryFilePath(string targetFilePath)
            {
                if (string.IsNullOrWhiteSpace(targetFilePath))
                {
                    throw new ArgumentException("Target file path cannot be empty.", nameof(targetFilePath));
                }

                var fullTargetPath = Path.GetFullPath(targetFilePath);
                var directory = Path.GetDirectoryName(fullTargetPath);
                var fileName = Path.GetFileName(fullTargetPath);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Target file path must include a file name.", nameof(targetFilePath));
                }

                return Path.Combine(directory, "." + fileName + "." + Guid.NewGuid().ToString("N") + ".tmp");
            }

            /// <summary>
            ///   <para>使用已经写好的临时文件替换目标文件。</para>
            /// </summary>
            /// <param name="temporaryFilePath">临时文件路径。</param>
            /// <param name="targetFilePath">最终目标文件路径。</param>
            public static void ReplaceFile(string temporaryFilePath, string targetFilePath)
            {
                if (string.IsNullOrWhiteSpace(temporaryFilePath))
                {
                    throw new ArgumentException("Temporary file path cannot be empty.", nameof(temporaryFilePath));
                }
                if (string.IsNullOrWhiteSpace(targetFilePath))
                {
                    throw new ArgumentException("Target file path cannot be empty.", nameof(targetFilePath));
                }

                if (File.Exists(targetFilePath))
                {
                    File.Replace(temporaryFilePath, targetFilePath, null);
                }
                else
                {
                    File.Move(temporaryFilePath, targetFilePath);
                }
            }
        }
    }
}