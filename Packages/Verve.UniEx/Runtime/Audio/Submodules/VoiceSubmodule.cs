#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace VerveUniEx.Audio
{
    /// <summary>
    /// 语音子模块 - 用于叙事与玩家沟通
    /// </summary>
    [System.Serializable]
    public class VoiceSubmodule : AudioSubmodule
    {
        public override string ModuleName => "Voice";
    }
}

#endif