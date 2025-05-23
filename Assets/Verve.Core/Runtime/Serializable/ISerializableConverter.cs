namespace Verve.Serializable
{
    using System.IO;

    
    public interface ISerializableConverter : Unit.IUnitService
    {
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(byte[] value);
        /// <summary>
        /// 系列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] Serialize(object obj);
        
        void Serialize(Stream stream, object obj);
        
        T DeserializeFromStream<T>(Stream stream);
    }
}