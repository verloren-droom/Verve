namespace Verve.Audio
{
    using Unit;
    using Loader;
    using System;
    using ObjectPool;
#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO
    using UnityEngine;
    using UnityEngine.Audio;
#endif
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 声音单元
    /// </summary>
    [CustomUnit("Audio"), System.Serializable]
    public sealed partial class AudioUnit : UnitBase
    {
        public override HashSet<Type> DependencyUnits { get; protected set; } = new HashSet<Type>()
        {
            typeof(LoaderUnit)
        };
        
        private LoaderUnit m_LoaderUnit;

#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO
        private AudioMixer m_Mixer;
        private AudioMixerGroup m_SfxGroup;
        private AudioMixerGroup m_MusicGroup;
        private AudioMixerGroup m_AmbientGroup;
#endif

// #if UNITY_5_3_OR_NEWER
//         [SerializeField]
// #endif
//         private float m_SfxVolume = 1.0f;
// #if UNITY_5_3_OR_NEWER
//         [SerializeField]
// #endif
//         private float m_MusicVolume = 1.0f;
// #if UNITY_5_3_OR_NEWER
//         [SerializeField]
// #endif
//         private float m_AmbientVolume = 1.0f;

#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif
        private float m_Volume = 1.0f;

        
        public float Volume
        {
            get => m_Volume;
            set
            {
#if UNITY_5_3_OR_NEWER
                m_Volume = Mathf.Clamp01(value);
#else
                m_Volume = Math.Clamp(value, 0, 1);
#endif
            }
        }

        private Dictionary<string, AudioAsset> m_SfxAudioAssets = new Dictionary<string, AudioAsset>();
        private Dictionary<string, AudioAsset> m_MusicAudioAssets = new Dictionary<string, AudioAsset>();
        private Dictionary<string, AudioAsset> m_AmbientAudioAssets = new Dictionary<string, AudioAsset>();

        private readonly ObjectPool<AudioAsset> m_AudioPool = new ObjectPool<AudioAsset>(
            () => AudioAsset.Create(null, false, 1.0f),
            null,
            asset =>
            {
                asset?.Stop();
                asset?.Reset();
            }, asset => asset?.Dispose());
        
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
            parent.onInitialized += (_) =>
            {
                parent.TryGetDependency<LoaderUnit>(out m_LoaderUnit);
            };
        }

        public void PlaySound<TLoaderType>(string audioPath, float delay = 0.0f) where TLoaderType : IAssetLoader
        {
            if (m_AudioPool.TryGet(out var audio))
            {
                audio.Clip = m_LoaderUnit.LoadAsset<TLoaderType, AudioClip>(audioPath);
                audio.Play(false, delay);
                audio.onStopped += () =>
                {
                    m_AudioPool.Release(audio);
                };
            }
        }

        public void PlaySound(AudioClip clip, float delay = 0.0f)
        {
            if (m_AudioPool.TryGet(out var audio))
            {
                audio.Clip = clip;
                audio.Play(false, delay);
                audio.onStopped += () =>
                {
                    m_AudioPool.Release(audio);
                };
            }
        }

        public void PlaySoundAtPosition<TLoaderType>(string audioPath, Vector3 target, float delay = 0.0f) where TLoaderType : IAssetLoader
        {
            if (!m_SfxAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                audioAsset = AudioAsset.Create(m_LoaderUnit.LoadAsset<TLoaderType, AudioClip>(audioPath), false, 1, m_SfxGroup);
            }
            
            audioAsset.Play(target, 1, false, delay);
        }

        public void PauseSound(string audioPath)
        {
            if (m_SfxAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                audioAsset.Pause();
            }
        }

        public void StopSound(string audioPath)
        {
            if (m_SfxAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                audioAsset.Stop();
            }
        }

        public void PauseAllSound()
        {
            foreach (var assetPath in m_SfxAudioAssets.Keys)
            {
                PauseSound(assetPath);
            }
        }

        public void StopAllSound()
        {
            foreach (var assetPath in m_SfxAudioAssets.Keys)
            {
                StopSound(assetPath);
            }
        }
        
        public void PlayMusic<TAssetType>(string audioPath) where TAssetType : IAssetLoader {}

        public void PauseMusic(string audioPath)
        {
            if (m_MusicAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                audioAsset.Pause();
            }
        }

        public void PauseAllMusic()
        {
            foreach (var assetPath in m_MusicAudioAssets.Keys)
            {
                PauseMusic(assetPath);
            }
        }

        public void StopMusic(string audioPath)
        {
            if (m_MusicAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                audioAsset.Stop();
            }
        }

        public void StopAllMusic()
        {
            foreach (var assetPath in m_MusicAudioAssets.Keys)
            {
                StopMusic(assetPath);
            }
        }
        
    }
}