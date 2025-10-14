#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    /// <summary>
    /// 音频功能模块 - 负责音频播放、混音控制及音量管理等
    /// </summary>
    [System.Serializable, GameFeatureModule("Verve/Audio", Description = "音频功能模块 - 负责音频播放、混音控制及音量管理等")]
    internal sealed class AudioGameFeature : GameFeatureModule
    {
        
    }
}

#endif