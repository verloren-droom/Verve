#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace VerveUniEx.Audio
{
    using Verve;
    using System;
    using Verve.Pool;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Audio;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;


    /// <summary>
    /// 音效子模块 - 提供操作反馈与环境代入感
    /// </summary>
    [System.Serializable]
    public partial class SoundEffectSubmodule : AudioSubmodule
    {
        [SerializeField, Tooltip("音频总音量"), Range(0, 1)] private float m_Volume = 1.0f;

        public override string ModuleName => "SoundEffect";
        
        private ObjectPool<AudioSource> m_Pool;
        [SerializeField, ReadOnly] private List<AudioSource> m_ActiveSources = new List<AudioSource>();

        public override float Volume
        {
            get => m_Volume;
            set
            {
                if (m_Volume == value) return;
                m_Volume = value;
            }
        }

        public override bool Mute { get => m_ActiveSources.Any(source => source.mute); set => m_ActiveSources.ForEach(source => source.mute = value); }
        public override bool IsPlaying => m_ActiveSources.Any(source => source.isPlaying);

        
        public override void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            m_Pool = new ObjectPool<AudioSource>(() =>
            {
                var source = m_Prefab ? GameObject.Instantiate(m_Prefab).GetComponent<AudioSource>() : new GameObject($"[{ModuleName}]").AddComponent<AudioSource>();
                source.outputAudioMixerGroup = m_Group;
                source.gameObject.SetActive(false);
                return source;
            }, source =>
            {
                source.gameObject.SetActive(true);
                m_ActiveSources.Add(source);
            }, source =>
            {
                source.Stop();
                source.clip = null;
                source.transform.position = Vector3.zero;
                source.transform.rotation = Quaternion.identity;
                source.gameObject.name = $"[{ModuleName}]";
                source.gameObject.SetActive(false);
                m_ActiveSources.Remove(source);
            }, source =>
            {
                m_ActiveSources.Remove(source);
                GameObject.Destroy(source.gameObject);
            });
        }

        public override void OnModuleUnloaded()
        {
            m_Pool.Clear();
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        public async Task Play(AudioClip clip, float volume = 1.0f, Vector3? target = null, float pitch = 1.0f, float minDistance = 1.0f, float maxDistance = 500.0f, float spatialBlend = 1.0f, float panStereo = 0.0f)
        {
            if (!m_Pool.TryGet(out var audio) || clip == null) return;
            
            audio.clip = clip;
            audio.gameObject.name = $"[{ModuleName}] {clip.name}";
            audio.spatialBlend = Mathf.Clamp01(spatialBlend);
            audio.minDistance = Mathf.Max(minDistance, 0);
            audio.maxDistance = maxDistance;
            audio.volume = Volume * Mathf.Clamp01(volume);
            audio.panStereo = Mathf.Clamp(panStereo, -1.0f, 1.0f);
            audio.pitch = Mathf.Clamp(pitch, -3.0f, 3.0f);
            if (target.HasValue)
            {
                audio.transform.position = target.Value;
            }
            audio.Play();
            if (!audio.loop)
            {
                await Task.Delay(TimeSpan.FromSeconds(clip.length * ((double) Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale)));
                m_Pool.Release(audio);
            }
        }

        public void Pause(AudioClip clip)
        {
            m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.Pause();
        }
        
        public void UnPause(AudioClip clip)
        {
            m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.UnPause();
        }

        public void PauseAll()
        {
            m_ActiveSources.ForEach(source => source.Pause());
        }

        public void UnPauseAll()
        {
            m_ActiveSources.ForEach(source => source.UnPause());
        }
        
        public void Stop(AudioClip clip)
        {
            m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.Stop();
        }
        
        public void StopAll()
        {
            foreach (var source in m_ActiveSources.ToArray())
            {
                source.Stop();
                m_Pool.Release(source);
            }
        }

        public void SetMute(AudioClip clip, bool mute)
        {
            var source = m_ActiveSources.FirstOrDefault(source => source.clip == clip);
            if (source != null)
            {
                source.mute = mute;
            }
        }

        public bool GetMute(AudioClip clip)
        {
            return m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.mute ?? false;
        }

        public void SetVolume(AudioClip clip, float volume)
        {
            var source = m_ActiveSources.FirstOrDefault(source => source.clip == clip);
            if (source != null)
            {
                source.volume = Volume * Mathf.Clamp01(volume);
            }
        }
        
        public float GetVolume(AudioClip clip)
        {
            return m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.volume ?? 0;
        }
        
        public bool ContainActiveClip(AudioClip clip)
        {
            return m_ActiveSources.Any(source => source.clip == clip);
        }

        public bool IsClipPlaying(AudioClip clip)
        {
            return m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.isPlaying ?? false;
        }
    }
}

#endif