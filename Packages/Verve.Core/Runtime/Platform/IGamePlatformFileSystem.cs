namespace Verve
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>游戏平台文件系统接口</para>
    /// </summary>
    public interface IGamePlatformFileSystem
    {
        /// <summary>
        ///   <para>项目路径</para>
        /// </summary>
        string ProjectPath { get; }
        
        /// <summary>
        ///   <para>持久化数据路径</para>
        /// </summary>
        string PersistentDataPath { get; }
        
        /// <summary>
        ///   <para>临时缓存路径</para>
        /// </summary>
        string TemporaryCachePath { get; }

        #region 文件操作
        
        /// <summary>
        ///   <para>检查指定路径的文件是否存在</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>
        ///   <para>如果文件存在则为 true，否则为 false</para>
        /// </returns>
        bool FileExists(string filePath);
        
        /// <summary>
        ///   <para>获取文件信息</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>
        ///   <para>文件信息</para>
        /// </returns>
        FileInfo GetFileInfo(string filePath);

        /// <summary>
        ///   <para>读取指定文件</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>
        ///   <para>文件内容的二进制数据</para>
        /// </returns>
        byte[] ReadFile(string filePath);
        
        /// <summary>
        ///   <para>写入指定文件</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="buffer">写入文件内容的二进制数据</param>
        /// <param name="overwrite">是否覆盖已存在文件，默认为 true</param>
        /// <returns>
        ///   <para>如果写入成功则为 true，否则为 false</para>
        /// </returns>
        bool WriteFile(string filePath, byte[] buffer, bool overwrite = true);
        
        /// <summary>
        ///   <para>重命名指定文件</para>
        /// </summary>
        /// <param name="oldFilePath">重命名前文件路径</param>
        /// <param name="newFilePath">重命名后文件路径</param>
        /// <returns>
        ///   <para>如果重命名成功则为 true，否则为 false</para>
        /// </returns>
        /// <exception cref="FileNotFoundException">当原文件不存在时抛出</exception>
        /// <exception cref="IOException">当目标文件已存在时抛出</exception>
        bool RenameFile(string oldFilePath, string newFilePath);
        
        /// <summary>
        ///   <para>删除指定文件</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>
        ///   <para>如果删除成功则为 true，否则为 false</para>
        /// </returns>
        /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
        bool DeleteFile(string filePath);
        
        /// <summary>
        ///   <para>复制文件</para>
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationFilePath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在文件</param>
        /// <returns>
        ///   <para>如果复制成功则为 true，否则为 false</para>
        /// </returns>
        /// <exception cref="FileNotFoundException">当源文件不存在时抛出</exception>
        /// <exception cref="IOException">当目标文件已存在且 overwrite 为 false 时抛出</exception>
        bool CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false);
        
        /// <summary>
        ///   <para>移动文件</para>
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationFilePath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在文件</param>
        /// <returns>
        ///   <para>如果移动成功则为 true，否则为 false</para>
        /// </returns>
        /// <exception cref="FileNotFoundException">当源文件不存在时抛出</exception>
        /// <exception cref="IOException">当目标文件已存在且 overwrite 为 false 时抛出</exception>
        bool MoveFile(string sourceFilePath, string destinationFilePath, bool overwrite = false);
    
        #endregion
    
        #region 目录操作
        
        /// <summary>
        ///   <para>检查指定路径的目录是否存在</para>
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>
        ///   <para>如果目录存在则为 true，否则为 false</para>
        /// </returns>
        bool DirectoryExists(string path);
        
        /// <summary>
        ///   <para>创建目录</para>
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>
        ///   <para>如果创建成功则为 true，否则为 false</para>
        /// </returns>
        bool CreateDirectory(string path);
        
        /// <summary>
        ///   <para>删除目录</para>
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="recursive">是否递归删除子目录和文件</param>
        /// <returns>
        ///   <para>如果删除成功则为 true，否则为 false</para>
        /// </returns>
        bool DeleteDirectory(string path, bool recursive = false);
        
        /// <summary>
        ///   <para>获取目录中的文件列表</para>
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="searchOption">搜索选项</param>
        /// <returns>
        ///   <para>文件路径枚举</para>
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
        IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        
        /// <summary>
        ///   <para>获取目录中的子目录列表</para>
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="searchOption">搜索选项</param>
        /// <returns>
        ///   <para>子目录路径枚举</para>
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
        IEnumerable<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    
        #endregion
    
        // #region 文件监视
        //
        // /// <summary>
        // ///   <para>监视指定路径的文件系统变化</para>
        // /// </summary>
        // /// <param name="path">要监视的路径</param>
        // /// <param name="filter">筛选器字符串，用于确定在目录中监视哪些文件</param>
        // /// <param name="changeTypes">要监视的变化类型</param>
        // /// <returns>可观察的文件系统事件序列</returns>
        // IObservable<FileSystemEventArgs> Watch(string path, string filter = "*.*", NotifyFilters changeTypes = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName);
        //
        // #endregion
    
        #region 异步操作
        
        /// <summary>
        /// 异步读取指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务，包含文件内容的二进制数据</returns>
        Task<byte[]> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 异步写入指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="buffer">写入文件内容的二进制数据</param>
        /// <param name="overwrite">是否覆盖已存在文件，默认为 true</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务，包含是否写入成功的结果</returns>
        Task<bool> WriteFileAsync(string filePath, byte[] buffer, bool overwrite = true, CancellationToken cancellationToken = default);
    
        #endregion
    }
}