namespace Verve.Storage
{
    using System;
    using ProtoBuf;
    using Serializable;
    using System.Collections.Concurrent;
    
    
    public sealed partial class BinaryStorage : StorageBase
    {
        private SerializableUnit m_Unit;

        public BinaryStorage(SerializableUnit unit)
        {
            m_Unit = unit;
        }

        public override bool TryRead<TData>(string fileName, string key, out TData outValue, TData defaultValue = default)
        {
            throw new NotImplementedException();
        }

        public override void Write<TData>(string fileName, string key, TData value)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string fileName, string key)
        {
            throw new NotImplementedException();
        }

        public override void DeleteAll(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}