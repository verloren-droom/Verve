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

        public virtual IDisposable Watch(string path, Action<FileSystemEventArgs> onChanged)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            FileSystemEventHandler handler = (sender, e) => onChanged?.Invoke(e);
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += (sender, e) => onChanged?.Invoke(e);

            // return Disposable.Create(() =>
            // {
            //     watcher.Changed -= handler;
            //     watcher.Created -= handler;
            //     watcher.Deleted -= handler;
            //     watcher.Dispose();
            // });
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
        
        public virtual IObservable<FileSystemEventArgs> Watch(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            return new ObservableFileWatcher(path);
        }

        private class ObservableFileWatcher : IObservable<FileSystemEventArgs>
        {
            private readonly string m_Path;

            public ObservableFileWatcher(string path)
            {
                m_Path = path;
            }

            public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
            {
                var watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(m_Path),
                    Filter = Path.GetFileName(m_Path),
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true
                };

                FileSystemEventHandler handler = (sender, e) => observer.OnNext(e);
                RenamedEventHandler renamedHandler = (sender, e) => observer.OnNext(e);
                ErrorEventHandler errorHandler = (sender, e) => observer.OnError(e.GetException());

                watcher.Changed += handler;
                watcher.Created += handler;
                watcher.Deleted += handler;
                watcher.Renamed += renamedHandler;
                watcher.Error += errorHandler;

                return new ActionDisposable(() =>
                {
                    watcher.Changed -= handler;
                    watcher.Created -= handler;
                    watcher.Deleted -= handler;
                    watcher.Renamed -= renamedHandler;
                    watcher.Error -= errorHandler;
                    watcher.Dispose();
                });
            }
        }
    }
}