#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    /// <summary>
    /// 音频子模块基类
    /// </summary>
    [System.Serializable]
    public abstract class AudioSubmodule : GameFeatureSubmodule<AudioGameFeatureComponent>, IAudio
    {
        public virtual float Volume { get; set; }
        public virtual bool IsPlaying { get; }
        public virtual bool Mute { get; set; }
    }
}

#endif