#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Serializable
{
    using ProtoBuf;
    using System.IO;


    /// <summary>
    /// ProtoBuf序列化 - 采用ProtoBuf库
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(SerializableGameFeature), Description = "ProtoBuf序列化 - 采用ProtoBuf库")]
    public sealed partial class ProtoBufSerializable : SerializableSubmodule
    {
        public override void Serialize(Stream stream, object obj)
        {
            Serializer.Serialize(stream, obj);
        }

        public override T Deserialize<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);
        }
    }
}

#endif