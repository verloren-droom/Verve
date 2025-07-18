#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace VerveUniEx.Audio
{
    using Verve;
    using UnityEngine;
    using UnityEngine.Audio;
    using System.Threading.Tasks;
    
    
    /// <summary>
    /// 音乐子模块
    /// </summary>
    [System.Serializable]
    public partial class MusicSubmodule : AudioSubmodule
    {
        public override string ModuleName => "Music";
        
        private AudioSource m_Source;

        public override bool Mute { get => m_Source.mute; set => m_Source.mute = value; }
        public override bool IsPlaying => m_Source?.isPlaying ?? false;
        public override float Volume
        {
            get => m_Source?.volume ?? 0.0f;
            set => m_Source.volume = value;
        }

        private float m_LastVolume = 1.0f;

        public override void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnModuleLoaded(dependencies);
            m_Source = m_Prefab != null ? GameObject.Instantiate(m_Prefab).GetComponent<AudioSource>() : new GameObject($"[{ModuleName}]").AddComponent<AudioSource>();
            m_Source.outputAudioMixerGroup = m_Group;
            m_Source.playOnAwake = false;
            m_Source.loop = true;
            GameObject.DontDestroyOnLoad(m_Source.gameObject);
        }

        public override void OnModuleUnloaded()
        {
            base.OnModuleUnloaded();
            GameObject.Destroy(m_Source.gameObject);
        }

        public async Task Play(AudioClip clip, float fadeDuration = 0)
        {
            if (clip == null || m_Source == null) return;

            if (m_Source.isPlaying)
            {
                await FadeOut(fadeDuration);
            }
            
            m_Source.clip = clip;
            m_Source.gameObject.name = $"[{ModuleName}] [{clip.name}]";
            m_Source.Play();
            await FadeIn(fadeDuration);
        }

        public void Stop()
        {
            if (m_Source == null || !m_Source.isPlaying) return;
            m_Source.Stop();
        }
        
        public void Pause()
        {
            if (m_Source == null || !m_Source.isPlaying) return;
            m_Source.Pause();
        }
        
        public void UnPause()
        {
            if (m_Source == null || !m_Source.isPlaying) return;
            m_Source.UnPause();
        }

        /// <summary>
        /// 淡入效果
        /// </summary>
        private async Task FadeIn(float duration)
        {
            float timer = 0;
            float startVolume = 0;
            float targetVolume = m_LastVolume;
        
            while (timer < duration)
            {
                timer += Time.deltaTime;
                Volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                await Task.Yield();
            }

            Volume = m_LastVolume;
        }
    
        /// <summary>
        /// 淡出效果
        /// </summary>
        public async Task FadeOut(float duration)
        {
            float timer = 0;
            m_LastVolume = Volume;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                Volume = Mathf.Lerp(m_LastVolume, 0, timer / duration);
                await Task.Yield();
            }

            m_Source.Stop();
        }
    }
}

#endif