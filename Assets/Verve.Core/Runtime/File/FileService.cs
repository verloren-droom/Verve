namespace Verve.File
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    

    public abstract class FileServiceBase : IFileService
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual Stream OpenRead(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }

        public virtual Stream OpenWrite(string path, FileMode mode = FileMode.Create)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            EnsureDirectoryExists(Path.GetDirectoryName(path));
            
            return new FileStream(
                path,
                mode,
                FileAccess.Write,
                FileShare.None);
        }

        public virtual IObservable<FileSystemEventArgs> Watch(string path)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<Stream> OpenReadAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            return await Task.FromResult(
                new FileStream(
                    path, 
                    FileMode.Open, 
                    FileAccess.Read, 
                    FileShare.Read,
                    bufferSize: 4096, 
                    useAsync: true));
        }

        public virtual async Task<Stream> OpenWriteAsync(string path, FileMode mode = FileMode.Create)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            EnsureDirectoryExists(Path.GetDirectoryName(path));
            
            return await Task.FromResult(
                new FileStream(
                    path, 
                    mode, 
                    FileAccess.Write, 
                    FileShare.None,
                    bufferSize: 4096, 
                    useAsync: true));
        }

        public abstract string PersistentDataPath { get; }
        public abstract string TemporaryPath { get; }
        
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}