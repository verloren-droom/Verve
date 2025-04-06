namespace Verve.Serializable
{
    public interface ISerializableConverter
    {
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Deserialize<T>(string value);
        
        /// <summary>
        /// 系列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        string Serialize(object obj);
    }
}