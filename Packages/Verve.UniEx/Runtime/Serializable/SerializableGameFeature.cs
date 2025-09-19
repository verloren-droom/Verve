#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Serializable
{
    /// <summary>
    /// 序列化游戏功能模块
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/Serializable", Description = "序列化游戏功能模块")]
    internal sealed class SerializableGameFeature : GameFeatureModule
    {
        
    }
}

#endif