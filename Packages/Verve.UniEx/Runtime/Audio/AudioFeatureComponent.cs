#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Audio
{
    using Verve;
    using System;
    using Loader;
    using UnityEngine;
    using UnityEngine.Audio;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 音频功能组件
    /// </summary>
    public partial class AudioFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("声效模块")] private SoundEffectSubmodule m_SoundEffect = new SoundEffectSubmodule();
        [SerializeField, Tooltip("音乐模块")] private MusicSubmodule m_Music = new MusicSubmodule();
        [SerializeField, Tooltip("语音模块")] private VoiceSubmodule m_Voice = new VoiceSubmodule();
        [SerializeField, Tooltip("音频混合器")] private AudioMixer m_Mixer;

        public SoundEffectSubmodule SoundEffect => m_SoundEffect;
        public MusicSubmodule Music => m_Music;
        public VoiceSubmodule Voice => m_Voice;
        public AudioMixer Mixer => m_Mixer;


        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            
            m_SoundEffect.OnModuleLoaded(dependencies);
            m_Music.OnModuleLoaded(dependencies);
            m_Voice.OnModuleLoaded(dependencies);
        }
    }
}

#endif