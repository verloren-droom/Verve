namespace Verve.File
{
    using IO;
    using System;
    using System.IO;

    
    /// <summary>
    /// 文件子模块接口
    /// </summary>
    public interface IFileSubmodule : IGameFeatureSubmodule, IIOProviderAsync
    {
        /// <summary>
        /// 文件监视
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IObservable<FileSystemEventArgs> Watch(string path);
    }
}