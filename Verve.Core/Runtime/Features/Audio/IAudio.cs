#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.Audio
{
    /// <summary>
    ///   <para>音频接口</para>
    /// </summary>
    public interface IAudio
    {
        /// <summary>
        ///   <para>音量</para>
        /// </summary>
        float Volume { get; set; }
        
        /// <summary>
        ///   <para>静音</para>
        /// </summary>
        bool Mute { get; set; }
        
        /// <summary>
        ///   <para>是否正在播放</para>
        /// </summary>
        bool IsPlaying { get; }
    }
}

#endif