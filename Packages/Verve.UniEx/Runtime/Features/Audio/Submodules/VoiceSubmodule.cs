#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    /// <summary>
    /// 语音子模块 - 用于叙事与玩家沟通
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(AudioGameFeature), Description = "语音子模块 - 用于叙事与玩家沟通")]
    public sealed partial class VoiceSubmodule : AudioSubmodule
    {
        // TODO: 实现语音子模块
    }
}

#endif