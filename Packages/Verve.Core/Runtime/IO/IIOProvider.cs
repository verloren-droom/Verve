namespace Verve.IO
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    

    public interface IIOProvider
    {
        Stream OpenRead(string path);
        Stream OpenWrite(string path, FileMode mode = FileMode.Create);
    }
    
    
    public interface IIOProviderAsync : IIOProvider
    {
        Task<Stream> OpenReadAsync(string path);
        Task<Stream> OpenWriteAsync(string path, FileMode mode = FileMode.Create);
    }
}