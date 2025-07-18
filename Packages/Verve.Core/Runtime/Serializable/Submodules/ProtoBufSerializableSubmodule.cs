namespace Verve.Serializable
{
    using ProtoBuf;
    using System.IO;


    /// <summary>
    /// ProtoBuf序列化子模块 - 采用ProtoBuf库
    /// </summary>
    public sealed partial class ProtoBufSerializableSubmodule : SerializableSubmodule
    {
        public override string ModuleName => "ProtoBufSerializable";

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