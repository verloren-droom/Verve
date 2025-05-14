namespace VerveUniEx.Audio
{
    
#if UNITY_5_3_OR_NEWER && ENABLE_AUDIO
    using Verve;
    using System;
    using UnityEngine;
    using UnityEngine.Audio;


    /// <summary>
    /// 音效资源 
    /// </summary>
    [Serializable]
    public sealed partial class AudioAsset : IDisposable
    {
        private readonly AudioSource m_Source;
        [SerializeField, PropertyDisable] private string m_AssetName;

        /// <summary>
        /// 音效被播放事件
        /// </summary>
        public event Action onPlayed;
        /// <summary>
        /// 音效被停止事件
        /// </summary>
        public event Action onStopped;
        /// <summary>
        /// 音效被静音事件
        /// </summary>
        public event Action onMuted;
        /// <summary>
        /// 音效被暂停事件
        /// </summary>
        public event Action onPaused;
        
        public float Volume
        {
            get => m_Source.volume;
            set  {
                m_Source.volume = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_Source.volume, 0))
                {
                    onMuted?.Invoke();
                }
            }
        }

        public bool Mute
        {
            get => m_Source.mute;
            set {
                m_Source.mute = value;
                if (m_Source.mute)
                {
                    onMuted?.Invoke();
                }
            }
        }

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => m_Source != null && m_Source.isPlaying;

        /// <summary>
        /// 资源名
        /// </summary>
        public string AssetName
        {
            set => m_Source.name = m_AssetName = $"Audio_{value ?? "<None Audio Clip>"}";
            private get
            {
                m_AssetName = m_Source?.clip?.name;
                return m_AssetName ?? "<None Audio Clip>";
            }
        }

        /// <summary>
        /// 循环
        /// </summary>
        public bool Loop
        {
            get => m_Source.loop;
            set => m_Source.loop = value;
        }

        /// <summary>
        /// 音频长度（秒）
        /// </summary>
        public float Length => m_Source.clip != null ? m_Source.clip.length : 0;

        
        /// <summary>
        /// 播放进度（0-1）
        /// </summary>
        public float Progress
        {
            get => m_Source.clip != null ? m_Source.time / m_Source.clip.length : 0;
            set => m_Source.time = m_Source.clip != null ? Mathf.Clamp01(value) * m_Source.clip.length : 0;
        }

        
        /// <summary>
        /// 空间混合（0=2D, 1=3D）
        /// </summary>
        public float SpatialBlend
        {
            get => m_Source.spatialBlend;
            set => m_Source.spatialBlend = Mathf.Clamp01(value);
        }


        /// <summary>
        /// 音调调整（-3到3）
        /// </summary>
        public float Pitch
        {
            get => m_Source.pitch;
            set => m_Source.pitch = Mathf.Clamp(value, -3, 3);
        }


        public AudioClip Clip
        {
            get => m_Source.clip;
            set
            {
                m_Source.clip = value;
                AssetName = value?.name;
            }
        }


        public AudioAsset(AudioClip clip, bool playOnAwake = false, float baseVolume = 1.0f, AudioMixerGroup mixerGroup = null, Transform parent = null)
        {
            m_Source = new GameObject($"Audio_{AssetName}").AddComponent<AudioSource>();
            m_Source.transform.SetParent(parent);
            Clip = clip;
            m_Source.playOnAwake = playOnAwake;
            m_Source.volume = baseVolume;
            m_Source.outputAudioMixerGroup = mixerGroup;
        }

        ~AudioAsset()
        {
            if (m_Source != null)
            {
                UnityEngine.GameObject.Destroy(m_Source);
            }
            Dispose();
        }
        
        public void Play(bool loop = false, float delay = .0f)
        {
            Play(Vector3.zero, 0, loop, delay);
        }
        
        public void Play(float volume, bool loop = false, float delay = .0f)
        {
            Volume = Mathf.Max(.0f, volume);
            Play(Vector3.zero, 0, loop, delay);
        }
        
        public void Play(Vector3 targetPosition, float spatialBlend = 1.0f, bool loop = false, float delay = .0f)
        {
            Play(targetPosition, 1, 500, AudioRolloffMode.Logarithmic, spatialBlend, loop, delay);
        }
        
        public async void Play(Vector3? targetPosition, float minDistance = 1, float maxDistance = 500, AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic, float spatialBlend = 1.0f, bool loop = false, float delay = .0f)
        {
            if (m_Source == null) return;
            m_Source?.gameObject.SetActive(true);
            if (targetPosition.HasValue)
            {
                m_Source.transform.position = targetPosition.Value;
            }
            // m_Source.transform.position = targetPosition;
            m_Source.minDistance = Mathf.Max(.0f, minDistance);
            m_Source.maxDistance = Mathf.Max(m_Source.minDistance, maxDistance);
            m_Source.rolloffMode = rolloffMode;
            SpatialBlend = Mathf.Clamp01(spatialBlend);
            Loop = loop;
            m_Source?.PlayDelayed(Mathf.Max(.0f, delay));
            onPlayed?.Invoke();
            await new WaitForSecondsRealtime(Length * ((double)Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
            onStopped?.Invoke();
        }

        public void Pause()
        {
            if (m_Source == null || !IsPlaying) return;
            m_Source?.Pause();
            onPaused?.Invoke();
        }

        public void Stop()
        {
            if (m_Source == null || !IsPlaying) return;
            m_Source?.Stop();
            onStopped?.Invoke();
        }

        public void UnPause()
        {
            if (m_Source == null || IsPlaying) return;
            m_Source?.UnPause();
            onPlayed?.Invoke();
        }

        public void Dispose()
        {
            Reset();
            onPlayed = null;
            onPaused = null;
            onStopped = null;
            onMuted = null;
        }

        public void Reset()
        {
            if (m_Source == null) return;
            Stop();
            Pitch = 1;
            m_Source.transform.position = Vector3.zero;
            m_Source?.gameObject.SetActive(false);
        }

        // public void LowPassFilter(float cutoffFrequency)
        // {
        //     var filter = m_Source.gameObject.GetComponent<AudioLowPassFilter>() ??
        //                  m_Source.gameObject.AddComponent<AudioLowPassFilter>();
        //     filter.cutoffFrequency = cutoffFrequency;
        // }

        public static AudioAsset Create(AudioClip clip, bool playOnAwake = false, float baseVolume = 1.0f, AudioMixerGroup mixerGroup = null, Transform parent = null)
        {
            return new AudioAsset(clip, playOnAwake, baseVolume, mixerGroup, parent);
        }
    }
#endif
    
}
