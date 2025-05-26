namespace Verve.Storage
{
    using System.Threading;
    using System.Threading.Tasks;

    
    public abstract class StorageBase : IStorage
    {
        public virtual string DefaultFileExtension { get; set; } = "";

        public abstract bool TryRead<TData>(string fileName, string key, out TData outValue,
            TData defaultValue = default);
        public abstract void Write<TData>(string fileName, string key, TData value);
        public abstract void Delete(string fileName, string key);
        public abstract void DeleteAll(string fileName);
        
        public virtual async Task<TData> ReadAsync<TData>(string fileName, string key, TData defaultValue = default, CancellationToken cancellationToken = default)
        {
            try
            {
                TData result = await Task.Run(() =>
                {
                    TryRead(fileName, key, out TData tempResult, defaultValue);
                    return tempResult;
                }, cancellationToken);
                return result;
            }
            catch
            {
                return defaultValue;
            }
        }

        public virtual async Task WriteAsync<TData>(string fileName, string key, TData value, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Run(() =>
                {
                    Write(fileName, key, value);
                }, cancellationToken);
            }
            catch { }
        }

        public virtual void Dispose() { }
    }
}