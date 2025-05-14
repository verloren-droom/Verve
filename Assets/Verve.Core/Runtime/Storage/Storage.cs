namespace Verve.Storage
{
    
    public abstract class StorageBase : IStorage
    {
        public abstract bool TryRead<TData>(string fileName, string key, out TData outValue,
            TData defaultValue = default);
        public abstract void Write<TData>(string fileName, string key, TData value);
        public abstract void Delete(string fileName, string key);
        public abstract void DeleteAll();
        
        public virtual void Dispose() { }
    }
    
}