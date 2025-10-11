#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    using UnityEngine;

    
    /// <summary>
    /// 音频子模块
    /// </summary>
    public interface IAudio
    {
        /// <summary> 音量 </summary>
        float Volume { get; set; }
        /// <summary> 静音 </summary>
        bool Mute { get; set; }
        /// <summary> 是否播放中 </summary>
        bool IsPlaying { get; }
    }
}

#endif