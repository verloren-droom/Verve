namespace Verve.Serializable
{
    using System.IO;
    
    
    /// <summary>
    ///   <para>序列化基类</para>
    /// </summary>
    public abstract class SerializableBase : ISerializable
    {
        public abstract void Serialize(Stream stream, object obj);
        public abstract T Deserialize<T>(Stream stream);
    }
}