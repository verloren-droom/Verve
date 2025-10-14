#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    using UnityEngine;
    using UnityEngine.Audio;
    
    
    /// <summary>
    /// 音频游戏功能组件
    /// </summary>
    [System.Serializable, GameFeatureComponentMenu("Verve/Audio")]
    public sealed class AudioGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("音频混合器")] private GameFeatureParameter<AudioMixer> m_Mixer = new GameFeatureParameter<AudioMixer>();
        [SerializeField, Tooltip("音乐分组")] private GameFeatureParameter<AudioMixerGroup> m_MusicGroup = new GameFeatureParameter<AudioMixerGroup>();
        [SerializeField, Tooltip("音效分组")] private GameFeatureParameter<AudioMixerGroup> m_SoundEffectGroup = new GameFeatureParameter<AudioMixerGroup>();
        [SerializeField, Tooltip("语音分组")] private GameFeatureParameter<AudioMixerGroup> m_VoiceGroup = new GameFeatureParameter<AudioMixerGroup>();
        
        
        /// <summary> 音频混合器 </summary>
        public AudioMixer Mixer => m_Mixer.Value;
        /// <summary> 音乐分组 </summary>
        public AudioMixerGroup MusicGroup => m_MusicGroup.Value;
        /// <summary> 音效分组 </summary>
        public AudioMixerGroup SoundEffectGroup => m_SoundEffectGroup.Value;
        /// <summary> 语音分组 </summary>
        public AudioMixerGroup VoiceGroup => m_VoiceGroup.Value;
    }
}

#endif