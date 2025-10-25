namespace Verve
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    

    /// <summary>
    /// 游戏平台文件系统接口
    /// </summary>
    public interface IGamePlatformFileSystem
    {
        /// <summary> 项目路径 </summary>
        string ProjectPath { get; }
        
        /// <summary> 持久化数据路径 </summary>
        string PersistentDataPath { get; }
        
        /// <summary> 临时缓存路径 </summary>
        string TemporaryCachePath { get; }

        #region 文件操作
        
        /// <summary>
        /// 检查指定路径的文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件是否存在</returns>
        bool FileExists(string filePath);
        
        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件信息</returns>
        /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
        FileInfo GetFileInfo(string filePath);

        /// <summary>
        /// 读取指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容的二进制数据</returns>
        /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
        byte[] ReadFile(string filePath);
        
        /// <summary>
        /// 写入指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="buffer">写入文件内容的二进制数据</param>
        /// <param name="overwrite">是否覆盖已存在文件，默认为 true</param>
        /// <returns>是否写入成功</returns>
        /// <exception cref="IOException">当文件已存在且 overwrite 为 false 时抛出</exception>
        bool WriteFile(string filePath, byte[] buffer, bool overwrite = true);
        
        /// <summary>
        /// 重命名指定文件
        /// </summary>
        /// <param name="oldFilePath">重命名前文件路径</param>
        /// <param name="newFilePath">重命名后文件路径</param>
        /// <returns>是否重命名成功</returns>
        /// <exception cref="FileNotFoundException">当原文件不存在时抛出</exception>
        /// <exception cref="IOException">当目标文件已存在时抛出</exception>
        bool RenameFile(string oldFilePath, string newFilePath);
        
        /// <summary>
        /// 删除指定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否删除成功</returns>
        /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
        bool DeleteFile(string filePath);
        
        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationFilePath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在文件</param>
        /// <returns>是否复制成功</returns>
        /// <exception cref="FileNotFoundException">当源文件不存在时抛出</exception>
        /// <exception cref="IOException">当目标文件已存在且 overwrite 为 false 时抛出</exception>
        bool CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false);
        
        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationFilePath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在文件</param>
        /// <returns>是否移动成功</returns>
        /// <exception cref="FileNotFoundException">当源文件不存在时抛出</exception>
        /// <exception cref="IOException">当目标文件已存在且 overwrite 为 false 时抛出</exception>
        bool MoveFile(string sourceFilePath, string destinationFilePath, bool overwrite = false);
    
        #endregion
    
        #region 目录操作
        
        /// <summary>
        /// 检查指定路径的目录是否存在
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>如果目录存在则为 true，否则为 false</returns>
        bool DirectoryExists(string path);
        
        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>是否创建成功</returns>
        bool CreateDirectory(string path);
        
        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="recursive">是否递归删除子目录和文件</param>
        /// <returns>是否删除成功</returns>
        bool DeleteDirectory(string path, bool recursive = false);
        
        /// <summary>
        /// 获取目录中的文件列表
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="searchOption">搜索选项</param>
        /// <returns>文件路径枚举</returns>
        /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
        IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        
        /// <summary>
        /// 获取目录中的子目录列表
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="searchOption">搜索选项</param>
        /// <returns>子目录路径枚举</returns>
        /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出</exception>
        IEnumerable<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    
        #endregion
    
        // #region 文件监视
        //
        // /// <summary>
        // /// 监视指定路径的文件系统变化
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