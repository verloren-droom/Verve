#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace VerveUniEx.Audio
{
    using Verve;
    using UnityEngine;
    using UnityEngine.Audio;
    
    
    [System.Serializable]
    public abstract class AudioSubmodule : IAudioSubmodule
    {
        [SerializeField, Tooltip("音频分组")] protected AudioMixerGroup m_Group;
        [SerializeField, RequireComponentOnGameObject(typeof(AudioSource))] protected GameObject m_Prefab;

        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }

        public virtual void OnModuleUnloaded() { }

        public virtual float Volume { get; set; }

        public virtual bool IsPlaying { get; }
        public virtual bool Mute { get; set; }
    }
}

#endif