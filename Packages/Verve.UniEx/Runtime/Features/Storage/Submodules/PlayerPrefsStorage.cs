#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx.Storage
{
    using UnityEngine;
    using System.Text;
    using Verve.Storage;
    
    
    /// <summary>
    /// PlayerPrefs 存储
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(StorageGameFeature), Description = "PlayerPrefs 存储")]
    public sealed partial class PlayerPrefsStorage : StorageSubmodule
    {
        public override string DefaultFileExtension { get; set; } = ".prefs";

        public bool TryReadData<TData>(
            string key,
            out TData outValue,
            Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer,
            TData defaultValue = default)
            => TryReadData(null, key, out outValue, encoding, deserializer, defaultValue);

        public override bool TryReadData<TData>(
            string filePath,
            string key,
            out TData outValue,
            Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer,
            TData defaultValue = default)
        {
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key))
            {
                outValue = defaultValue;
                return false;
            }

            outValue = deserializer(encoding.GetBytes(PlayerPrefs.GetString(key)));
            return outValue != null;
        }

        public void WriteData<TData>(
            string key,
            TData value,
            Encoding encoding,
            IStorage.SerializerDelegate serializer) 
            => WriteData(null, key, value, encoding, serializer);

        public override void WriteData<TData>(
            string filePath,
            string key,
            TData value,
            Encoding encoding,
            IStorage.SerializerDelegate serializer,
            IStorage.DeserializerDelegate<TData> deserializer = null)
        {
            if (string.IsNullOrEmpty(key)) return;
            PlayerPrefs.SetString(key, encoding.GetString(serializer(value)));
        }

        public void DeleteData(string key)
            => DeleteData<string>(null, key, Encoding.UTF8, null);

        public override void DeleteData<TData>(string filePath, string key, Encoding encoding,
            IStorage.DeserializerDelegate<TData> deserializer)
        {
            if (string.IsNullOrEmpty(key)) return;
            PlayerPrefs.DeleteKey(key);
        }

        public void DeleteAllData() => DeleteAllData(null);

        public override void DeleteAllData(string filePath)
        {
            PlayerPrefs.DeleteAll();
        }
        
        public bool HasData(string key) => HasData(null, key, null);

        public override bool HasData(string filePath, string key, IStorage.DeserializerDelegate<object> deserializer)
        {
            return PlayerPrefs.HasKey(key);
        }
    }
}
    
#endif