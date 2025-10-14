#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Serializable
{
    using System.IO;
    using Verve.Serializable;
    
    
    /// <summary>
    /// 序列化子模块基类
    /// </summary>
    [System.Serializable]
    public abstract class SerializableSubmodule : GameFeatureSubmodule, ISerializable
    {
        public abstract void Serialize(Stream stream, object obj);
        public abstract T Deserialize<T>(Stream stream);
    }
}

#endif