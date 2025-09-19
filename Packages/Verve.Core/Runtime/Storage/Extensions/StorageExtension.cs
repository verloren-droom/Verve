// namespace Verve.Storage
// {
//     using System.Text;
//
//     
//     /// <summary>
//     /// 存储扩展类 - 用于简化存储操作
//     /// </summary>
//     public static class StorageExtension
//     {
//         /// <summary>
//         /// 尝试读取数据
//         /// </summary>
//         public static bool TryReadData<TData>(
//             this IStorage self,
//             string filePath,
//             string key,
//             out TData outValue,
//             IStorage.DeserializerDelegate<TData> deserializer,
//             TData defaultValue = default)
//         {
//             return self.TryReadData(filePath, key, out outValue, Encoding.UTF8, deserializer, defaultValue);
//         }
//         
//         /// <summary>
//         /// 写入数据
//         /// </summary>
//         public static void WriteData<TData>(
//             this IStorage self,
//             string filePath,
//             string key,
//             TData value,
//             IStorage.SerializerDelegate serializer,
//             IStorage.DeserializerDelegate<TData> deserializer)
//         {
//             self.WriteData(filePath, key, value, Encoding.UTF8, serializer, deserializer);
//         }
//
//         /// <summary>
//         /// 删除数据
//         /// </summary>
//         public static void DeleteData(
//             this IStorage self,
//             string filePath, 
//             string key,
//             IStorage.DeserializerDelegate<object> deserializer)
//         {
//             self.DeleteData(filePath, key, Encoding.UTF8, deserializer);
//         }
//     }
// }