namespace Verve.File
{
    using IO;
    using System;
    using System.IO;

    
    public interface IFileService : Unit.IUnitService, IIOProviderAsync
    {
        /// <summary>
        /// 持久化资源路径
        /// </summary>
        string PersistentDataPath { get; }
        /// <summary>
        /// 临时资源路径
        /// </summary>
        string TemporaryPath { get; }
        /// <summary>
        /// 文件监视
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IObservable<FileSystemEventArgs> Watch(string path);
    }
}