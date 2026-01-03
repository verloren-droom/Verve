#if UNITY_5_3_OR_NEWER

namespace Verve.Serializable
{
    /// <summary>
    ///   <para>序列化游戏功能模块</para>
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/Serializable", Description = "序列化游戏功能模块")]
    internal sealed class SerializableGameFeature : GameFeatureModule
    {
        
    }
}

#endif