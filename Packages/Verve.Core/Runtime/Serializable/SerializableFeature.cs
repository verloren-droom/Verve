namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    
    
    /// <summary>
    /// 序列化功能
    /// </summary>
    [System.Serializable]
    public class SerializableFeature : ModularGameFeature
    {
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            // RegisterSubmodule(new CsvSerializableService());
            RegisterSubmodule(new ProtoBufSerializableSubmodule());
            RegisterSubmodule(new JsonSerializableSubmodule());
            
            base.OnLoad(dependencies);
        }
    }
}