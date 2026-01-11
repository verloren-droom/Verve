#if UNITY_5_3_OR_NEWER

namespace Verve.Serializable
{
    using ProtoBuf;
    using System.IO;


    /// <summary>
    ///   <para>ProtoBuf序列化</para>
    ///   <para>采用ProtoBuf库</para>
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(SerializableGameFeature), Description = "ProtoBuf序列化 - 采用ProtoBuf库")]
    public sealed partial class ProtoBufSerializer : SerializerSubmodule
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