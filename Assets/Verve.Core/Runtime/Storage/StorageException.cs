namespace Verve.Storage
{
    
    using System;
    
    
    public class StorageException : Exception
    {
        public StorageException(string message, Exception inner) : base(message, inner) { }
    }
    
}