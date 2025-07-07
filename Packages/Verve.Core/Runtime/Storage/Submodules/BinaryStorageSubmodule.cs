namespace Verve.Storage
{
    using System;
    using ProtoBuf;
    using Serializable;
    using System.Collections.Concurrent;
    
    
    /// <summary>
    /// 二进制存储子模块
    /// </summary>
    public sealed partial class BinaryStorageSubmodule : StorageSubmodule
    {
        public override string ModuleName => "BinaryStorage";
        
        public override string DefaultFileExtension { get; set; } = ".bin";

        private SerializableFeature m_Serializable;

        public BinaryStorageSubmodule(SerializableFeature serializable)
        {
            m_Serializable = serializable;
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