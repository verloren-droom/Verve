namespace Verve.File
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 文件系统基类
    /// </summary>
    public abstract class FileSystemBase : IFileSystem
    {
        public string RootPath { get; set; }
        
        #region 文件操作

        public abstract bool FileExists(string filePath);
        public abstract FileInfo GetFileInfo(string filePath);
        public virtual IEnumerable<FileInfo> GetFileInfos(IEnumerable<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                yield return GetFileInfo(filePath);
            }
        }
        public abstract byte[] ReadFile(string filePath);
        public abstract bool WriteFile(string filePath, byte[] buffer, bool overwrite = true);
        public abstract bool RenameFile(string oldFilePath, string newFilePath);
        public abstract bool DeleteFile(string filePath);
        public abstract bool CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false);
        public abstract bool MoveFile(string sourceFilePath, string destinationFilePath, bool overwrite = false);
        
        #endregion

        #region 目录操作
        
        public abstract bool DirectoryExists(string path);
        public abstract bool CreateDirectory(string path);
        public abstract bool DeleteDirectory(string path, bool recursive = false);
        public abstract IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        public abstract IEnumerable<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        
        #endregion
    
        #region 异步操作
        
        public abstract Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
        public abstract Task<bool> WriteFileAsync(string filePath, byte[] buffer, bool overwrite = true, CancellationToken cancellationToken = default);
        
        #endregion

        #region 辅助方法

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        protected virtual void EnsureDirectoryExists(string directoryPath)
        {
            var directory = Path.GetDirectoryName(directoryPath);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }
        }

        #endregion
    }
}