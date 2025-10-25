#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.IO;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// <para>通用文件系统</para>
    /// </summary>
    internal class GenericFileSystem : IGamePlatformFileSystem
    {
        public virtual string ProjectPath => Application.dataPath;

        public virtual string PersistentDataPath => 
#if UNITY_EDITOR
            Path.Combine(ProjectPath, "..", "Temp", ".Cache");
#else
            Application.persistentDataPath;
#endif
        
        public virtual string TemporaryCachePath => Application.temporaryCachePath;

        public virtual bool FileExists(string filePath)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);
            return File.Exists(filePath);
        }

        public virtual FileInfo GetFileInfo(string filePath)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);
            return new FileInfo(filePath);
        }

        public virtual byte[] ReadFile(string filePath)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);

            if (!FileExists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            byte[] buffer = new byte[fileStream.Length];
            fileStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public virtual bool WriteFile(string filePath, byte[] buffer, bool overwrite = true)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
                
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);

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

        public virtual bool RenameFile(string oldFilePath, string newFilePath)
        {
            oldFilePath = Path.IsPathRooted(oldFilePath) ? oldFilePath : Path.Combine(PersistentDataPath, oldFilePath);

            if (!FileExists(oldFilePath))
                throw new FileNotFoundException("Source file not found: ", oldFilePath);
                
            newFilePath = Path.IsPathRooted(newFilePath) ? newFilePath : Path.Combine(PersistentDataPath, newFilePath);
            if (FileExists(newFilePath))
                File.Delete(newFilePath);
                
            File.Move(oldFilePath, newFilePath);
            return true;
        }

        public virtual bool DeleteFile(string filePath)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);
            if (!FileExists(filePath))
                return false;
                
            File.Delete(filePath);
            return true;
        }

        public virtual bool CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
        {
            sourceFilePath = Path.IsPathRooted(sourceFilePath) ? sourceFilePath : Path.Combine(PersistentDataPath, sourceFilePath);
            if (!FileExists(sourceFilePath))
                throw new FileNotFoundException("Source file not found: ", sourceFilePath);
            
            destinationFilePath = Path.IsPathRooted(destinationFilePath) ? destinationFilePath : Path.Combine(PersistentDataPath, destinationFilePath);
            string directory = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
                
            File.Copy(sourceFilePath, destinationFilePath, overwrite);
            return true;
        }

        public virtual bool MoveFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
        {
            sourceFilePath = Path.IsPathRooted(sourceFilePath) ? sourceFilePath : Path.Combine(PersistentDataPath, sourceFilePath);
            if (!FileExists(sourceFilePath))
                throw new FileNotFoundException("Source file not found: ", sourceFilePath);
            
            destinationFilePath = Path.IsPathRooted(destinationFilePath) ? destinationFilePath : Path.Combine(PersistentDataPath, destinationFilePath);
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

        public virtual bool DirectoryExists(string path)
        {
            path = Path.IsPathRooted(path) ? path : Path.Combine(PersistentDataPath, path);
            return Directory.Exists(path);
        }

        public virtual bool CreateDirectory(string path)
        {
            path = Path.IsPathRooted(path) ? path : Path.Combine(PersistentDataPath, path);
            if (DirectoryExists(path))
                return false;
                
            Directory.CreateDirectory(path);
            return true;
        }

        public virtual bool DeleteDirectory(string path, bool recursive = false)
        {
            path = Path.IsPathRooted(path) ? path : Path.Combine(PersistentDataPath, path);
            if (!DirectoryExists(path))
                return false;
                
            Directory.Delete(path, recursive);
            return true;
        }

        public virtual IEnumerable<string> GetFiles(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            path = Path.IsPathRooted(path) ? path : Path.Combine(PersistentDataPath, path);
            if (!DirectoryExists(path))
                throw new DirectoryNotFoundException("目录未找到: " + path);
                
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public virtual IEnumerable<string> GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            path = Path.IsPathRooted(path) ? path : Path.Combine(PersistentDataPath, path);
            if (!DirectoryExists(path))
                throw new DirectoryNotFoundException("Directory not found: " + path);
                
            return Directory.GetDirectories(path, searchPattern, searchOption);
        }

        public virtual async Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);
            if (!FileExists(filePath))
                throw new FileNotFoundException("File not found: ", filePath);
                
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                byte[] buffer = new byte[fileStream.Length];
                await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                return buffer;
            }
        }

        public virtual async Task<bool> WriteFileAsync(
            string filePath, byte[] buffer, bool overwrite = true,
            CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            
            filePath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(PersistentDataPath, filePath);
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
        protected string GetTempFilePath(string targetFilePath)
        {
            string fileName = Path.GetFileName(targetFilePath);
            string tempFileName = $".{fileName}.{Guid.NewGuid():N}.tmp";
            return Path.Combine(TemporaryCachePath, tempFileName);
        }
    }
}

#endif