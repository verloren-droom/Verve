#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Audio
{
    using Verve;
    using System;
    using Loader;
    using Verve.Pool;
    using UnityEngine;
    using UnityEngine.Audio;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 音频功能组件
    /// </summary>
    public partial class AudioFeatureComponent : GameFeatureComponent
    {
        private LoaderFeature m_Loader;
        
        [SerializeField, Tooltip("音频混合器")] private AudioMixer m_Mixer;
        [SerializeField, Tooltip("音频分组-音效")] private AudioMixerGroup m_SfxGroup;
        [SerializeField, Tooltip("音频分组-音乐")] private AudioMixerGroup m_MusicGroup;
        [SerializeField, Tooltip("音频分组-环境")] private AudioMixerGroup m_AmbientGroup;
        
        [SerializeField, Range(0, 1), Tooltip("音效音量")] private float m_SfxVolume = 1.0f;
        [SerializeField, Range(0, 1), Tooltip("音乐音量")] private float m_MusicVolume = 1.0f;
        [SerializeField, Range(0, 1), Tooltip("环境音量")] private float m_AmbientVolume = 1.0f;
        [SerializeField, Range(0, 1), Tooltip("总音量")] private float m_Volume = 1.0f;
        
        public float SfxVolume
        {
            get => m_SfxVolume;
            set => m_SfxVolume = Mathf.Clamp01(value) * Volume;
        }
        
        public float MusicVolume
        {
            get => m_MusicVolume;
            set => m_MusicVolume = Mathf.Clamp01(value) * Volume;
        }
        
        public float AmbientVolume
        {
            get => m_AmbientVolume;
            set => m_AmbientVolume = Mathf.Clamp01(value) * Volume;
        }
        
        public float Volume
        {
            get => m_Volume;
            set => m_Volume = Mathf.Clamp01(value);
        }
        
        private Dictionary<string, AudioAsset> m_SfxAudioAssets = new Dictionary<string, AudioAsset>();
        private Dictionary<string, AudioAsset> m_MusicAudioAssets = new Dictionary<string, AudioAsset>();
        private Dictionary<string, AudioAsset> m_AmbientAudioAssets = new Dictionary<string, AudioAsset>();
        
        private ObjectPool<AudioAsset> m_SfxAudioPool;
        private ObjectPool<AudioAsset> m_MusicAudioPool;
        private ObjectPool<AudioAsset> m_AmbientAudioPool;
        
        
        protected override void OnLoad(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            m_Loader = dependencies.Get<LoaderFeature>();
            base.OnLoad(dependencies);
            
            if (!Application.isPlaying) return;

            m_SfxAudioPool = new ObjectPool<AudioAsset>(
                () => AudioAsset.Create(null, false, 1.0f, m_SfxGroup, null),
                null,
                asset =>
                {
                    asset?.Stop();
                    asset?.Reset();
                }, asset => asset?.Dispose());
            m_MusicAudioPool = new ObjectPool<AudioAsset>(
                () => AudioAsset.Create(null, false, 1.0f, m_MusicGroup, null),
                null,
                asset =>
                {
                    asset?.Stop();
                    asset?.Reset();
                }, asset => asset?.Dispose());
            m_AmbientAudioPool = new ObjectPool<AudioAsset>(
                () => AudioAsset.Create(null, false, 1.0f, m_AmbientGroup, null),
                null,
                asset =>
                {
                    asset?.Stop();
                    asset?.Reset();
                }, asset => asset?.Dispose());
        }
        
        public void PreloadSound<TLoaderType>(string audioPath) where TLoaderType : class, IAssetLoader
        {
            if (string.IsNullOrEmpty(audioPath))
            {
                throw new ArgumentNullException(nameof(audioPath));
            }
        
            var clip = m_Loader.LoadAsset<TLoaderType, AudioClip>(audioPath);
            if (clip == null)
            {
                return;
            }
        
            if (!m_SfxAudioAssets.ContainsKey(audioPath))
            {
                m_SfxAudioAssets[audioPath] = AudioAsset.Create(clip, false, 1.0f, m_SfxGroup);
            }
        }
        
        public void PlaySound<TLoaderType>(string audioPath, float delay = 0.0f) where TLoaderType : class, IAssetLoader
        {
            if (string.IsNullOrEmpty(audioPath))
            {
                throw new ArgumentNullException(nameof(audioPath));
            }
        
            if (m_SfxAudioPool.TryGet(out var audio))
            {
                var clip = m_Loader.LoadAsset<TLoaderType, AudioClip>(audioPath);
                if (clip == null)
                {
                    return;
                }
        
                audio.Clip = clip;
                audio.Play(SfxVolume,false,  delay);
                audio.onStopped += () =>
                {
                    m_SfxAudioPool.Release(audio);
                };
            }
        }
        
        public void PlaySound(AudioClip clip, float delay = 0.0f)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }
        
            if (m_SfxAudioPool.TryGet(out var audio))
            {
                audio.Clip = clip;
                audio.Play(SfxVolume,false,  delay);
                audio.onStopped += () =>
                {
                    m_SfxAudioPool.Release(audio);
                };
            }
        }
        
        public void PlaySoundAtPosition<TLoaderType>(string audioPath, Vector3 target, float delay = 0.0f) where TLoaderType : class, IAssetLoader
        {
            if (string.IsNullOrEmpty(audioPath))
            {
                throw new ArgumentNullException(nameof(audioPath));
            }
        
            if (!m_SfxAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                var clip = m_Loader.LoadAsset<TLoaderType, AudioClip>(audioPath);
                if (clip == null)
                {
                    return;
                }
        
                audioAsset = AudioAsset.Create(clip, false, 1, m_SfxGroup);
            }
        
            audioAsset.Play(target, 1, false, delay);
        }
        
        public void PauseSound(string audioPath)
        {
            if (!string.IsNullOrEmpty(audioPath) && m_SfxAudioAssets.TryGetValue(audioPath, out var audioAsset))
            {
                audioAsset.Pause();
            }
        }
        
        public void StopSound(string audioPath)
        {
            if (!string.IsNullOrEmpty(audioPath) && m_SfxAudioAssets.TryGetValue(audioPath, out var audioAsset))
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
        
        public void PlayMusic<TAssetType>(string audioPath) where TAssetType : class, IAssetLoader
        {
            if (!string.IsNullOrEmpty(audioPath) && m_MusicAudioPool.TryGet(out var audio))
            {
                audio.Clip = m_Loader.LoadAsset<TAssetType, AudioClip>(audioPath);
                audio.Play(MusicVolume, true, 0.0f);
                audio.onStopped += () =>
                {
                    m_MusicAudioPool.Release(audio);
                };
            }
        }
        
        public void PauseMusic(string audioPath)
        {
            if (!string.IsNullOrEmpty(audioPath) && m_MusicAudioAssets.TryGetValue(audioPath, out var audioAsset))
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
            if (!string.IsNullOrEmpty(audioPath) && m_MusicAudioAssets.TryGetValue(audioPath, out var audioAsset))
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

#endif