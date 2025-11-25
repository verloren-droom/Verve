namespace Verve.Storage
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    
    /// <summary>
    ///   <para>存储基类</para>
    /// </summary>
    public abstract class StorageBase : IStorage
    {
        public abstract string DefaultFileExtension { get; set; }
        
        public abstract bool TryReadData<TData>(string filePath, string key, out TData outValue, Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer, TData defaultValue = default);

        public abstract void WriteData<TData>(string filePath, string key, TData value, Encoding encoding,
            IStorage.SerializerDelegate serializer, IStorage.DeserializerDelegate<TData> deserializer);


        public abstract void DeleteData<TData>(string filePath, string key, Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer);
        
        public abstract void DeleteAllData(string filePath);
        public abstract bool HasData(string filePath, string key, IStorage.DeserializerDelegate<object> deserializer);

        public virtual async Task<TData> ReadDataAsync<TData>(
            string filePath,
            string key,
            Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer,
            TData defaultValue = default,
            CancellationToken ct = default)
        {
            try
            {
                TData result = await Task.Run(() =>
                {
                    TryReadData(filePath, key, out TData tempResult, encoding, deserializer, defaultValue);
                    return tempResult;
                }, ct);
                return result;
            }
            catch
            {
                return defaultValue;
            }
        }

        public virtual async Task WriteDataAsync<TData>(
            string filePath,
            string key,
            TData value,
            Encoding encoding,
            IStorage.SerializerDelegate serializer,
            IStorage.DeserializerDelegate<TData> deserializer,
            CancellationToken ct = default)
        {
            try
            {
                await Task.Run(() =>
                {
                    WriteData(filePath, key, value, encoding, serializer, deserializer);
                }, ct);
            }
            catch { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}