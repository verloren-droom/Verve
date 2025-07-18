namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    
    
    /// <summary>
    /// 序列化功能
    /// </summary>
    [System.Serializable]
    public class SerializableFeature : ModularGameFeature<ISerializableSubmodule>
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new ProtoBufSerializableSubmodule());
            RegisterSubmodule(new JsonSerializableSubmodule());
        }
    }
}