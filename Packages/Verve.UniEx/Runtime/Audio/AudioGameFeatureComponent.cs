#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    using UnityEngine;
    using UnityEngine.Audio;
    
    
    /// <summary>
    /// 音频游戏功能组件
    /// </summary>
    [GameFeatureComponentMenu("Verve/Audio")]
    public sealed class AudioGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("音频混合器")] private GameFeatureParameter<AudioMixer> m_Mixer = new GameFeatureParameter<AudioMixer>();
        [SerializeField, Tooltip("音频分组")] private GameFeatureParameter<AudioMixerGroup> m_MusicGroup = new GameFeatureParameter<AudioMixerGroup>();
        [SerializeField, Tooltip("音频分组")] private GameFeatureParameter<AudioMixerGroup> m_SoundEffectGroup = new GameFeatureParameter<AudioMixerGroup>();
        [SerializeField, Tooltip("音频分组")] private GameFeatureParameter<AudioMixerGroup> m_VoiceGroup = new GameFeatureParameter<AudioMixerGroup>();
        
        
        public AudioMixer Mixer => m_Mixer.Value;
        public AudioMixerGroup MusicGroup => m_MusicGroup.Value;
        public AudioMixerGroup SoundEffectGroup => m_SoundEffectGroup.Value;
        public AudioMixerGroup VoiceGroup => m_VoiceGroup.Value;
    }
}

#endif