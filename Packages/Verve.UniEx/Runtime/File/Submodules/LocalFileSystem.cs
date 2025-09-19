#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.File
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 本地文件系统
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(FileGameFeature), Description = "本地文件系统")]
    public sealed partial class LocalFileSystem : FileSystemSubmodule
    {
        public override bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public override FileInfo GetFileInfo(string filePath)
        {
            return new FileInfo(filePath);
        }

        public override byte[] ReadFile(string filePath)
        {
            if (!FileExists(filePath))
                throw new FileNotFoundException("File not found", filePath);
                
            return File.ReadAllBytes(filePath);
        }

        public override bool WriteFile(string filePath, byte[] buffer, bool overwrite = true)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
                
            if (FileExists(filePath) && !overwrite)
                return false;
                
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            string tempFilePath = GetTempFilePath(filePath);
            try
            {
                File.WriteAllBytes(tempFilePath, buffer);
                
                if (FileExists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
                
                return true;
            }
            finally
            {
                if (FileExists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }

        public override bool RenameFile(string oldFilePath, string newFilePath)
        {
            if (!FileExists(oldFilePath))
                throw new FileNotFoundException("Source file not found: ", oldFilePath);
                
            if (FileExists(newFilePath))
                File.Delete(newFilePath);
                
            File.Move(oldFilePath, newFilePath);
            return true;
        }

        public override bool DeleteFile(string filePath)
        {
            if (!FileExists(filePath))
                return false;
                
            File.Delete(filePath);
            return true;
        }

        public override bool CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
        {
            if (!FileExists(sourceFilePath))
                throw new FileNotFoundException("Source file not found: ", sourceFilePath);
                
            string directory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            File.Copy(sourceFilePath, destinationFilePath, overwrite);
            return true;
        }

        public override bool MoveFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
        {
            if (!FileExists(sourceFilePath))
                throw new FileNotFoundException("Source file not found: ", sourceFilePath);
                
            if (FileExists(destinationFilePath))
            {
                if (!overwrite)
                    return false;
                File.Delete(destinationFilePath);
            }
            
            string directory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            File.Move(sourceFilePath, destinationFilePath);
            return true;
        }

        public override bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public override bool CreateDirectory(string path)
        {
            if (DirectoryExists(path))
                return false;
                
            Directory.CreateDirectory(path);
            return true;
        }

        public override bool DeleteDirectory(string path, bool recursive = false)
        {
            if (!DirectoryExists(path))
                return false;
                
            Directory.Delete(path, recursive);
            return true;
        }

        public override IEnumerable<string> GetFiles(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!DirectoryExists(path))
                throw new DirectoryNotFoundException("目录未找到: " + path);
                
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public override IEnumerable<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!DirectoryExists(path))
                throw new DirectoryNotFoundException("Directory not found: " + path);
                
            return Directory.GetDirectories(path, searchPattern, searchOption);
        }

        public override async Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!FileExists(filePath))
                throw new FileNotFoundException("File not found: ", filePath);
                
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                return buffer;
            }
        }

        public override async Task<bool> WriteFileAsync(
            string filePath, byte[] buffer, bool overwrite = true,
            CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
                
            if (FileExists(filePath) && !overwrite)
                return false;
                
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            string tempFilePath = GetTempFilePath(filePath);
            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await fileStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                }
                
                if (FileExists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
                
                return true;
            }
            finally
            {
                if (FileExists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }
        
        /// <summary>
        /// 生成临时文件路径
        /// </summary>
        private string GetTempFilePath(string targetFilePath)
        {
            string directory = Path.GetDirectoryName(targetFilePath);
            string fileName = Path.GetFileName(targetFilePath);
            string tempFileName = $".{fileName}.{Guid.NewGuid():N}.tmp";
            return Path.Combine(directory, tempFileName);
        }
    }
}

#endif
