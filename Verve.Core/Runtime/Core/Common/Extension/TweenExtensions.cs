#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    

    /// <summary>
    ///   <para>缓动动画函数</para>
    /// </summary>
    public delegate float TweenEasingFunction(float t);

    /// <summary>
    ///   <para>预设缓动动画函数</para>
    /// </summary>
    public static class TweenEasePreset
    {
        /// <summary>
        ///   <para>线性缓动</para>
        /// </summary>
        public static readonly TweenEasingFunction Linear = t => t;
        
        /// <summary>
        ///   <para>二次方缓入函数</para>
        ///   <para>开始缓慢，然后加速</para>
        /// </summary>
        public static readonly TweenEasingFunction InQuad = t => t * t;
        
        /// <summary>
        ///   <para>二次方缓出函数</para>
        ///   <para>开始快速，然后减速</para>
        /// </summary>
        public static readonly TweenEasingFunction OutQuad = t => 1 - (1 - t) * (1 - t);
        
        /// <summary>
        ///   <para>二次方缓入缓出函数</para>
        ///   <para>开始缓慢，中间加速，结束前减速</para>
        /// </summary>
        public static readonly TweenEasingFunction InOutQuad = t => t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        
        /// <summary>
        ///   <para>三次方缓入函数</para>
        ///   <para>比二次方缓入更加平缓开始，然后更快加速</para>
        /// </summary>
        public static readonly TweenEasingFunction InCubic = t => t * t * t;
        
        /// <summary>
        ///   <para>三次方缓出函数</para>
        ///   <para>比二次方缓出更快开始，然后更急剧减速</para>
        /// </summary>
        public static readonly TweenEasingFunction OutCubic = t => 1 - Mathf.Pow(1 - t, 3);
        
        /// <summary>
        ///   <para>三次方缓入缓出函数</para>
        ///   <para>开始更平缓，中间更快加速，结束前更急剧减速</para>
        /// </summary>
        public static readonly TweenEasingFunction InOutCubic = t => t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
    
    /// <summary>
    ///   <para>缓动动画动作接口</para>
    /// </summary>
    public interface ITweenAction : IDisposable
    {
        /// <summary>
        ///   <para>执行缓动动画</para>
        /// </summary>
        IEnumerator Execute();
        
        /// <summary>
        ///   <para>停止缓动动画</para>
        /// </summary>
        void Stop();
        
        /// <summary>
        ///   <para>动画是否正在运行</para>
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    ///   <para>基础值缓动动画</para>
    /// </summary>
    public class ValueTween<T> : ITweenAction
    {
        private readonly ITweenValueSource<T> m_ValueSource;
        private readonly T m_StartValue;
        private readonly T m_EndValue;
        private readonly float m_Duration;
        private readonly TweenEasingFunction m_Easing;
        private readonly Func<T, T, float, T> m_LerpFunction;
        private readonly TweenValueChangeHandler<T> m_OnValueChanged;
        private bool m_IsRunning;
        private bool m_UseUnscaledTime;
    
        public ValueTween(
            ITweenValueSource<T> tweenValueSource,
            T endValue,
            float duration,
            TweenEasingFunction tweenEasing,
            Func<T, T, float, T> lerpFunction,
            TweenValueChangeHandler<T> onTweenValueChanged = null,
            bool useUnscaledTime = false)
        {
            m_ValueSource = tweenValueSource ?? throw new ArgumentNullException(nameof(tweenValueSource));
            m_StartValue = tweenValueSource.GetValue();
            m_EndValue = endValue;
            m_Duration = Mathf.Max(0, duration);
            m_Easing = tweenEasing ?? TweenEasePreset.Linear;
            m_LerpFunction = lerpFunction;
            m_OnValueChanged = onTweenValueChanged;
            m_UseUnscaledTime = useUnscaledTime;
        }
        
        public bool IsRunning => m_IsRunning;
    
        public IEnumerator Execute()
        {
            if (m_IsRunning) yield break;
            
            m_IsRunning = true;
            var elapsed = 0f;
            
            if (m_Duration <= 0)
            {
                m_ValueSource.SetValue(m_EndValue);
                m_OnValueChanged?.Invoke(m_EndValue);
                yield break;
            }

            while (elapsed < m_Duration && m_IsRunning)
            {
                elapsed += m_UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / m_Duration);
                var easedT = m_Easing(t);
                
                var newValue = m_LerpFunction(m_StartValue, m_EndValue, easedT);
                m_ValueSource.SetValue(newValue);
                m_OnValueChanged?.Invoke(newValue);
                
                yield return null;
            }
    
            if (m_IsRunning)
            {
                m_ValueSource.SetValue(m_EndValue);
                m_OnValueChanged?.Invoke(m_EndValue);
            }
            m_IsRunning = false;
        }
    
        public void Stop()
        {
            m_IsRunning = false;
        }
        
        void IDisposable.Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    ///   <para>缓动动画构建器</para>
    /// </summary>
    public class TweenBuilder : IDisposable
    {
        private readonly MonoBehaviour m_Runner;
        private readonly List<ITweenAction> m_Actions = new List<ITweenAction>();
        private Coroutine m_CurrentCoroutine;
        private bool m_IsPlaying;
        private bool m_IsDisposed;
    
        public event Action OnComplete;
        public event Action OnStopped;
    
        internal TweenBuilder(MonoBehaviour runner)
        {
            m_Runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }
    
        /// <summary>
        ///   <para>添加自定义缓动动画动作</para>
        /// </summary>
        public TweenBuilder AddAction(ITweenAction action)
        {
            if (!m_IsDisposed && action != null)
            {
                m_Actions.Add(action);
            }
            return this;
        }
    
        /// <summary>
        ///   <para>开始播放缓动动画</para>
        /// </summary>
        public TweenBuilder Play()
        {
            if (!m_IsPlaying && m_Runner != null && !m_IsDisposed)
            {
                m_IsPlaying = true;
                m_CurrentCoroutine = m_Runner.StartCoroutine(ExecuteAnimations());
            }
            return this;
        }
    
        /// <summary>
        ///   <para>停止缓动动画</para>
        /// </summary>
        public void Stop()
        {
            if (m_IsPlaying)
            {
                m_IsPlaying = false;
                if (m_CurrentCoroutine != null && m_Runner != null)
                {
                    m_Runner.StopCoroutine(m_CurrentCoroutine);
                    m_CurrentCoroutine = null;
                }
    
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.Stop();
                }
                
                OnStopped?.Invoke();
            }
        }
    
        private IEnumerator ExecuteAnimations()
        {
            for (int i = 0; i < m_Actions.Count; i++)
            {
                if (!m_IsPlaying) break;
                if (m_Actions[i] != null)
                {
                    yield return m_Actions[i].Execute();
                }
            }
            
            m_IsPlaying = false;
            m_CurrentCoroutine = null;
            
            if (!m_IsDisposed)
            {
                OnComplete?.Invoke();
            }
        }
    
        void IDisposable.Dispose()
        {
            if (!m_IsDisposed)
            {
                Stop();
                
                for (int i = 0; i < m_Actions.Count; i++)
                {
                    m_Actions[i]?.Dispose();
                }
                m_Actions.Clear();
                
                m_IsDisposed = true;
            }
        }
    }

    /// <summary>
    ///   <para>值动画源接口</para>
    ///   <para>提供值的获取和设置</para>
    /// </summary>
    public interface ITweenValueSource<T>
    {
        /// <summary>
        ///   <para>获取当前值</para>
        /// </summary>
        T GetValue();
        
        /// <summary>
        ///   <para>设置当前值</para>
        /// </summary>
        void SetValue(T value);
    }

    internal class TransformPositionSource : ITweenValueSource<Vector3>
    {
        private readonly Transform m_Transform;
        private readonly bool m_UseLocal;

        public TransformPositionSource(Transform transform, bool useLocal = false)
        {
            m_Transform = transform;
            m_UseLocal = useLocal;
        }

        public Vector3 GetValue() => m_UseLocal ? m_Transform.localPosition : m_Transform.position;
        public void SetValue(Vector3 value)
        {
            if (m_UseLocal)
                m_Transform.localPosition = value;
            else
                m_Transform.position = value;
        }
    }

    internal class TransformRotationSource : ITweenValueSource<Quaternion>
    {
        private readonly Transform m_Transform;
        private readonly bool m_UseLocal;

        public TransformRotationSource(Transform transform, bool useLocal = false)
        {
            m_Transform = transform;
            m_UseLocal = useLocal;
        }

        public Quaternion GetValue() => m_UseLocal ? m_Transform.localRotation : m_Transform.rotation;
        public void SetValue(Quaternion value)
        {
            if (m_UseLocal)
                m_Transform.localRotation = value;
            else
                m_Transform.rotation = value;
        }
    }

    internal class TransformScaleSource : ITweenValueSource<Vector3>
    {
        private readonly Transform m_Transform;
        public TransformScaleSource(Transform transform) => m_Transform = transform;
        public Vector3 GetValue() => m_Transform.localScale;
        public void SetValue(Vector3 value) => m_Transform.localScale = value;
    }
    
    internal class GraphicColorSource : ITweenValueSource<Color>
    {
        private readonly UnityEngine.UI.Graphic m_Graphic;
    
        public GraphicColorSource(UnityEngine.UI.Graphic graphic) => m_Graphic = graphic;
        public Color GetValue() => m_Graphic.color;
        public void SetValue(Color value) => m_Graphic.color = value;
    }
    
    internal class RectTransformAnchorSource : ITweenValueSource<Vector2>
    {
        private readonly RectTransform m_RectTransform;
        private readonly bool m_IsAnchoredPosition;

        public RectTransformAnchorSource(RectTransform rectTransform, bool isAnchoredPosition = true)
        {
            m_RectTransform = rectTransform;
            m_IsAnchoredPosition = isAnchoredPosition;
        }

        public Vector2 GetValue() => m_IsAnchoredPosition ? m_RectTransform.anchoredPosition : m_RectTransform.anchorMax;
        public void SetValue(Vector2 value)
        {
            if (m_RectTransform != null)
            {
                if (m_IsAnchoredPosition)
                    m_RectTransform.anchoredPosition = value;
                else
                    m_RectTransform.anchorMax = value;
            }
        }
    }
    
    /// <summary>
    ///   <para>值变化委托</para>
    /// </summary>
    public delegate void TweenValueChangeHandler<in T>(T value);
    
    /// <summary>
    ///   <para>缓动动画扩展方法</para>
    /// </summary>
    public static class TweenExtensions
    {
        /// <summary>
        ///   <para>移动到指定位置</para>
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveTo(
            this MonoBehaviour self,
            Vector3 targetPosition,
            float duration, 
            TweenEasingFunction tweenEasing = null,
            bool useLocal = false,
            bool useUnscaledTime = false)
        {
            var source = new TransformPositionSource(self.transform, useLocal);
            var tween = new ValueTween<Vector3>(source, targetPosition, duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }

        /// <summary>
        ///   <para>移动到指定位置</para>
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveTo(
            this MonoBehaviour self,
            Vector3 targetPosition,
            float duration, 
            AnimationCurve easing,
            bool useLocal = false,
            bool useUnscaledTime = false)
            => self.MoveTo(targetPosition, duration, easing.Evaluate, useLocal, useUnscaledTime);

        /// <summary>
        ///   <para>移动X轴到指定位置</para>
        /// </summary>
        /// <param name="targetX">目标X轴位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveXTo(
            this MonoBehaviour self,
            float targetX,
            float duration,
            TweenEasingFunction tweenEasing = null,
            bool useLocal = false,
            bool useUnscaledTime = false)
        {
            var source = new TransformPositionSource(self.transform, useLocal);
            var tween = new ValueTween<Vector3>(source, new Vector3(targetX, source.GetValue().y, source.GetValue().z), duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>移动X轴到指定位置</para>
        /// </summary>
        /// <param name="targetX">目标X轴位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveXTo(
            this MonoBehaviour self,
            float targetX,
            float duration,
            AnimationCurve easing,
            bool useLocal = false,
            bool useUnscaledTime = false)
            => self.MoveXTo(targetX, duration, easing.Evaluate, useLocal, useUnscaledTime);
        
        /// <summary>
        ///   <para>移动Y轴到指定位置</para>
        /// </summary>
        /// <param name="targetY">目标Y轴位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveYTo(
            this MonoBehaviour self,
            float targetY,
            float duration,
            TweenEasingFunction tweenEasing = null,
            bool useLocal = false,
            bool useUnscaledTime = false)
        {
            var source = new TransformPositionSource(self.transform, useLocal);
            var tween = new ValueTween<Vector3>(source, new Vector3(source.GetValue().x, targetY, source.GetValue().z), duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>移动Y轴到指定位置</para>
        /// </summary>
        /// <param name="targetY">目标Y轴位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveYTo(
            this MonoBehaviour self,
            float targetY,
            float duration,
            AnimationCurve easing,
            bool useLocal = false,
            bool useUnscaledTime = false)
            => self.MoveYTo(targetY, duration, easing.Evaluate, useLocal, useUnscaledTime);
        
        /// <summary>
        ///   <para>移动Z轴到指定位置</para>
        /// </summary>
        /// <param name="targetZ">目标Z轴位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveZTo(
            this MonoBehaviour self,
            float targetZ,
            float duration,
            TweenEasingFunction tweenEasing = null,
            bool useLocal = false,
            bool useUnscaledTime = false)
        {
            var source = new TransformPositionSource(self.transform, useLocal);
            var tween = new ValueTween<Vector3>(source, new Vector3(source.GetValue().x, source.GetValue().y, targetZ), duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>移动Z轴到指定位置</para>
        /// </summary>
        /// <param name="targetZ">目标Z轴位置</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder MoveZTo(
            this MonoBehaviour self,
            float targetZ,
            float duration,
            AnimationCurve easing,
            bool useLocal = false,
            bool useUnscaledTime = false)
            => self.MoveZTo(targetZ, duration, easing.Evaluate, useLocal, useUnscaledTime);
        
        /// <summary>
        ///   <para>旋转到指定角度</para>
        /// </summary>
        /// <param name="targetRotation">目标角度</param>
        /// <param name="duration">持续时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder RotateTo(
            this MonoBehaviour self,
            Quaternion targetRotation,
            float duration, 
            TweenEasingFunction tweenEasing = null,
            bool useLocal = false,
            bool useUnscaledTime = false)
        {
            var source = new TransformRotationSource(self.transform, useLocal);
            var tween = new ValueTween<Quaternion>(source, targetRotation, duration, tweenEasing, Quaternion.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>旋转到指定角度</para>
        /// </summary>
        /// <param name="targetRotation">目标角度</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useLocal">是否使用本地坐标</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder RotateTo(
            this MonoBehaviour self,
            Quaternion targetRotation,
            float duration,
            AnimationCurve easing,
            bool useLocal = false,
            bool useUnscaledTime = false)
            => self.RotateTo(targetRotation, duration, easing.Evaluate, useLocal, useUnscaledTime);

        /// <summary>
        ///   <para>缩放到指定大小</para>
        /// </summary>
        /// <param name="targetScale">目标大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleTo(
            this MonoBehaviour self,
            Vector3 targetScale,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new TransformScaleSource(self.transform);
            var tween = new ValueTween<Vector3>(source, targetScale, duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>缩放到指定大小</para>
        /// </summary>
        /// <param name="targetScale">目标大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleTo(
            this MonoBehaviour self,
            Vector3 targetScale,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ScaleTo(targetScale, duration, useUnscaledTime, easing.Evaluate);

        /// <summary>
        ///   <para>X轴缩放到指定大小</para>
        /// </summary>
        /// <param name="targetX">目标X轴大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleXTo(
            this MonoBehaviour self,
            float targetX,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new TransformScaleSource(self.transform);
            var tween = new ValueTween<Vector3>(source, new Vector3(targetX, source.GetValue().y, source.GetValue().z), duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>X轴缩放到指定大小</para>
        /// </summary>
        /// <param name="targetX">目标X轴大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleXTo(
            this MonoBehaviour self,
            float targetX,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ScaleXTo(targetX, duration, useUnscaledTime, easing.Evaluate);
        
        /// <summary>
        ///   <para>Y轴缩放到指定大小</para>
        /// </summary>
        /// <param name="targetY">目标Y轴大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleYTo(
            this MonoBehaviour self,
            float targetY,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new TransformScaleSource(self.transform);
            var tween = new ValueTween<Vector3>(source, new Vector3(source.GetValue().x, targetY, source.GetValue().z), duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>Y轴缩放到指定大小</para>
        /// </summary>
        /// <param name="targetY">目标Y轴大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleYTo(
            this MonoBehaviour self,
            float targetY,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ScaleYTo(targetY, duration, useUnscaledTime, easing.Evaluate);
        
        /// <summary>
        ///   <para>Z轴缩放到指定大小</para>
        /// </summary>
        /// <param name="targetZ">目标Z轴大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleZTo(
            this MonoBehaviour self,
            float targetZ,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new TransformScaleSource(self.transform);
            var tween = new ValueTween<Vector3>(source, new Vector3(source.GetValue().x, source.GetValue().y, targetZ), duration, tweenEasing, Vector3.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>Z轴缩放到指定大小</para>
        /// </summary>
        /// <param name="targetZ">目标Z轴大小</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ScaleZTo(
            this MonoBehaviour self,
            float targetZ,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ScaleZTo(targetZ, duration, useUnscaledTime, easing.Evaluate);

        /// <summary>
        ///   <para>颜色渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColor">目标颜色</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorTo(
            this UnityEngine.UI.Graphic self,
            Color targetColor,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new GraphicColorSource(self);
            var tween = new ValueTween<Color>(source, targetColor, duration, tweenEasing, Color.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>颜色渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColor">目标颜色</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorTo(
            this UnityEngine.UI.Graphic self,
            Color targetColor,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ColorTo(targetColor, duration, useUnscaledTime, easing.Evaluate);

        /// <summary>
        ///   <para>颜色R值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorR">目标颜色R值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorRTo(this UnityEngine.UI.Graphic self,
            float targetColorR,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new GraphicColorSource(self);
            var tween = new ValueTween<Color>(source, new Color(targetColorR, source.GetValue().g, source.GetValue().b), duration, tweenEasing, Color.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>颜色R值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorR">目标颜色R值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorRTo(
            this UnityEngine.UI.Graphic self,
            float targetColorR,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ColorRTo(targetColorR, duration, useUnscaledTime, easing.Evaluate);

        /// <summary>
        ///   <para>颜色G值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorG">目标颜色G值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorGTo(
            this UnityEngine.UI.Graphic self,
            float targetColorG,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new GraphicColorSource(self);
            var tween = new ValueTween<Color>(source, new Color(source.GetValue().r, targetColorG, source.GetValue().b), duration, tweenEasing, Color.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>颜色G值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorG">目标颜色G值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorGTo(
            this UnityEngine.UI.Graphic self,
            float targetColorG,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ColorGTo(targetColorG, duration, useUnscaledTime, easing.Evaluate);

        /// <summary>
        ///   <para>颜色B值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorB">目标颜色B值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorBTo(
            this UnityEngine.UI.Graphic self,
            float targetColorB,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new GraphicColorSource(self);
            var tween = new ValueTween<Color>(source, new Color(source.GetValue().r, source.GetValue().g, targetColorB), duration, tweenEasing, Color.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>颜色B值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorB">目标颜色B值</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorBTo(
            this UnityEngine.UI.Graphic self,
            float targetColorB,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.ColorBTo(targetColorB, duration, useUnscaledTime, easing.Evaluate);

        /// <summary>
        ///   <para>颜色A值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorA">目标透明度</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <param name="tweenEasing">缓动函数</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorATo(
            this UnityEngine.UI.Graphic graphic,
            float targetColorA,
            float duration,
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new GraphicColorSource(graphic);
            var endAlpha = Mathf.Clamp01(targetColorA);
            var tween = new ValueTween<Color>(source, new Color(source.GetValue().r, source.GetValue().g, source.GetValue().b, endAlpha), duration, tweenEasing, Color.Lerp, null, useUnscaledTime);
            return new TweenBuilder(graphic).AddAction(tween).Play();
        }
        
        /// <summary>
        ///   <para>颜色A值渐变到指定颜色</para>
        /// </summary>
        /// <param name="targetColorA">目标透明度</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        /// <returns>
        ///   <para>动画构建器</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder ColorATo(
            this UnityEngine.UI.Graphic graphic,
            float targetColorA,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => graphic.ColorATo(targetColorA, duration, useUnscaledTime, easing.Evaluate);
        
        /// <summary>
        ///   <para>锚点移动到指定锚点</para>
        /// </summary>
        /// <param name="targetAnchor">目标锚点</param>
        /// <param name="duration">持续时间</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder AnchorTo(
            this RectTransform self,
            Vector2 targetAnchor,
            float duration, 
            bool useUnscaledTime = false,
            TweenEasingFunction tweenEasing = null)
        {
            var source = new RectTransformAnchorSource(self);
            var tween = new ValueTween<Vector2>(source, targetAnchor, duration, tweenEasing, Vector2.Lerp, null, useUnscaledTime);
            return new TweenBuilder(self.GetComponent<MonoBehaviour>()).AddAction(tween).Play();
        }

        /// <summary>
        ///   <para>锚点移动到指定锚点</para>
        /// </summary>
        /// <param name="targetAnchor">目标锚点</param>
        /// <param name="duration">持续时间</param>
        /// <param name="easing">缓动函数曲线</param>
        /// <param name="useUnscaledTime">是否使用非缩放时间</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenBuilder AnchorTo(
            this RectTransform self,
            Vector2 targetAnchor,
            float duration,
            AnimationCurve easing,
            bool useUnscaledTime = false)
            => self.AnchorTo(targetAnchor, duration, useUnscaledTime, easing.Evaluate);
    }
}

#endif