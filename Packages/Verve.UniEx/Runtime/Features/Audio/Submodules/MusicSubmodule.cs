#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    using UnityEngine;
    using UnityEngine.Audio;
    using System.Collections;
    using System.Threading.Tasks;
    
    
    /// <summary>
    /// 音乐子模块 - 提供背景音乐播放与控制
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(AudioGameFeature), Description = "音乐子模块 - 提供背景音乐播放与控制")]
    public sealed partial class MusicSubmodule : AudioSubmodule
    {
        private AudioSource m_Source;

        public override bool Mute { get => m_Source.mute; set => m_Source.mute = value; }
        public override bool IsPlaying => m_Source?.isPlaying ?? false;
        public override float Volume
        {
            get => m_Source?.volume ?? 0.0f;
            set => m_Source.volume = value;
        }

        private float m_LastVolume = 1.0f;

        protected override IEnumerator OnStartup()
        {
            if (Application.isPlaying)
            {
                m_Source = new GameObject($"[{nameof(MusicSubmodule)}]").AddComponent<AudioSource>();
                m_Source.outputAudioMixerGroup = Component.MusicGroup;
                m_Source.playOnAwake = false;
                m_Source.loop = true;
                GameObject.DontDestroyOnLoad(m_Source.gameObject);
            }

            yield return null;
        }

        protected override void OnShutdown()
        {
            if (m_Source != null)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(m_Source);
                }
                else
                {
                    GameObject.DestroyImmediate(m_Source);
                }
            }
        }

        [ModuleMethodBridge("LoaderGameFeature.AddressablesLoader.LoadAsset")]
        [ModuleMethodBridge("LoaderGameFeature.AddressablesLoader.LoadAssetAsync")]
        public async Task Play([BridgeParameter] AudioClip clip, float fadeDuration = 0)
        {
            if (clip == null || m_Source == null) return;

            if (m_Source.isPlaying)
            {
                await FadeOut(fadeDuration);
            }
            
            m_Source.clip = clip;
            m_Source.gameObject.name = $"[{nameof(MusicSubmodule)}] [{clip.name}]";
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
        private async Task FadeOut(float duration)
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