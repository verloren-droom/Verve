#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO

namespace Verve.UniEx.Audio
{
    using Verve;
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Audio;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;


    /// <summary>
    ///   <para>音效子模块</para>
    ///   <para>提供操作反馈与环境代入感</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(AudioGameFeature), Description = "音效子模块 - 提供操作反馈与环境代入感")]
    public sealed partial class SoundEffectSubmodule : AudioSubmodule
    {
        [SerializeField, Tooltip("音频总音量"), Range(0, 1)] private float m_Volume = 1.0f;
        
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

        protected override IEnumerator OnStartup()
        {
            if (Application.isPlaying)
            {
                m_Pool = new ObjectPool<AudioSource>(() =>
                {
                    var source = new GameObject($"[{nameof(SoundEffectSubmodule)}]").AddComponent<AudioSource>();
                    source.outputAudioMixerGroup = Component.SoundEffectGroup;
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
                    source.gameObject.name = $"[{nameof(SoundEffectSubmodule)}]";
                    source.gameObject.SetActive(false);
                    m_ActiveSources.Remove(source);
                }, source =>
                {
                    m_ActiveSources.Remove(source);
                    GameObject.Destroy(source.gameObject);
                });
            }
            yield return null;
        }

        protected override void OnShutdown()
        {
            m_Pool?.Clear();
            base.OnShutdown();
        }

        /// <summary>
        ///   <para>播放音效</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="volume">音量</param>
        /// <param name="target">目标位置</param>
        /// <param name="pitch">音调</param>
        /// <param name="minDistance">最小距离</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="spatialBlend">空间混合</param>
        /// <param name="panStereo">立体声</param>
        public async Task Play(AudioClip clip, float volume = 1.0f, Vector3? target = null, float pitch = 1.0f, float minDistance = 1.0f, float maxDistance = 500.0f, float spatialBlend = 1.0f, float panStereo = 0.0f)
        {
            if (!m_Pool.TryGet(out var audio) || clip == null) return;
            
            audio.clip = clip;
            audio.gameObject.name = $"[{nameof(SoundEffectSubmodule)}] {clip.name}";
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

        /// <summary>
        ///   <para>暂停播放</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        public void Pause(AudioClip clip)
        {
            m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.Pause();
        }

        /// <summary>
        ///   <para>恢复播放</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        public void UnPause(AudioClip clip)
        {
            m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.UnPause();
        }

        /// <summary>
        ///   <para>暂停所有播放</para>
        /// </summary>
        public void PauseAll()
        {
            m_ActiveSources.ForEach(source => source.Pause());
        }

        /// <summary>
        ///   <para>恢复所有播放</para>
        /// </summary>
        public void UnPauseAll()
        {
            m_ActiveSources.ForEach(source => source.UnPause());
        }
        
        /// <summary>
        ///   <para>停止播放</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        public void Stop(AudioClip clip)
        {
            m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.Stop();
        }

        /// <summary>
        ///   <para>停止所有播放</para>
        /// </summary>
        public void StopAll()
        {
            foreach (var source in m_ActiveSources.ToArray())
            {
                source.Stop();
                m_Pool.Release(source);
            }
        }

        /// <summary>
        ///   <para>设置静音</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="mute">静音</param>
        public void SetMute(AudioClip clip, bool mute)
        {
            var source = m_ActiveSources.FirstOrDefault(source => source.clip == clip);
            if (source != null)
            {
                source.mute = mute;
            }
        }

        /// <summary>
        ///   <para>获取静音</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <returns>
        ///   <para>静音</para>
        /// </returns>
        public bool GetMute(AudioClip clip)
        {
            return m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.mute ?? false;
        }

        /// <summary>
        ///   <para>设置音量</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="volume">音量</param>
        public void SetVolume(AudioClip clip, float volume)
        {
            var source = m_ActiveSources.FirstOrDefault(source => source.clip == clip);
            if (source != null)
            {
                source.volume = Volume * Mathf.Clamp01(volume);
            }
        }

        /// <summary>
        ///   <para>获取音量</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <returns>
        ///   <para>音量</para>
        /// </returns>
        public float GetVolume(AudioClip clip)
        {
            return m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.volume ?? 0;
        }

        /// <summary>
        ///   <para>判断音频片段是否有效</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <returns>
        ///   <para>音频片段是否有效</para>
        /// </returns>
        public bool HasActiveClip(AudioClip clip)
        {
            return m_ActiveSources.Any(source => source.clip == clip);
        }

        /// <summary>
        ///   <para>判断音频片段是否正在播放</para>
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <returns>
        ///   <para>音频片段是否正在播放</para>
        /// </returns>
        public bool IsClipPlaying(AudioClip clip)
        {
            return m_ActiveSources.FirstOrDefault(source => source.clip == clip)?.isPlaying ?? false;
        }
    }
}

#endif