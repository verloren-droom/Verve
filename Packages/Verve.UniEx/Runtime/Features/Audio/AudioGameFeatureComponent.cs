#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    using UnityEngine;
    using UnityEngine.Audio;
    
    
    /// <summary>
    ///   <para>音频游戏功能组件</para>
    /// </summary>
    [System.Serializable, GameFeatureComponentMenu("Verve/Audio")]
    public sealed class AudioGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("音频混合器")] private GameFeatureParameter<AudioMixer> m_Mixer = new GameFeatureParameter<AudioMixer>();
        [SerializeField, Tooltip("音乐分组")] private GameFeatureParameter<AudioMixerGroup> m_MusicGroup = new GameFeatureParameter<AudioMixerGroup>();
        [SerializeField, Tooltip("音效分组")] private GameFeatureParameter<AudioMixerGroup> m_SoundEffectGroup = new GameFeatureParameter<AudioMixerGroup>();
        [SerializeField, Tooltip("语音分组")] private GameFeatureParameter<AudioMixerGroup> m_VoiceGroup = new GameFeatureParameter<AudioMixerGroup>();
        
        
        /// <summary>
        ///   <para>音频混合器</para>
        /// </summary>
        public AudioMixer Mixer => m_Mixer.Value;

        /// <summary>
        ///   <para>音乐分组</para>
        /// </summary>
        public AudioMixerGroup MusicGroup => m_MusicGroup.Value;
        
        /// <summary>
        ///   <para>音效分组</para>
        /// </summary>
        public AudioMixerGroup SoundEffectGroup => m_SoundEffectGroup.Value;
        
        /// <summary>
        ///   <para>语音分组</para>
        /// </summary>
        public AudioMixerGroup VoiceGroup => m_VoiceGroup.Value;
    }
}

#endif