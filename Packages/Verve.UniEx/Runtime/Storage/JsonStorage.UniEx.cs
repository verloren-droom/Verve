#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Storage
{
    using File;
    using Verve.Serializable;

    
    public class JsonStorage : Verve.Storage.JsonStorage
    {
        protected internal JsonStorage(SerializableUnit serializableUnit, FileUnit fileUnit) : base(serializableUnit, fileUnit)
        {
            
        }
    }
}

#endif