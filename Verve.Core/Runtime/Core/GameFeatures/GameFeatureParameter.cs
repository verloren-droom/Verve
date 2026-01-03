#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>游戏功能参数基类</para>
    /// </summary>
    [Serializable]
    public abstract class GameFeatureParameter : IGameFeatureParameter
    {
        [NonSerialized] protected internal bool m_Overrides;
        
        public bool Overrides
        {
            get => m_Overrides;
            set => m_Overrides = value;
        }
        
        public T GetValue<T>() => ((GameFeatureParameter<T>)this).Value;
        public abstract void SetValue(IGameFeatureParameter parameter);
        public abstract void ApplyOverride(IGameFeatureParameter other);
        
        /// <summary> 参数生命周期 </summary>
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void Release() { }
    }

    
    /// <summary>
    ///   <para>游戏功能参数基类</para>
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    [Serializable]
    public class GameFeatureParameter<T> : GameFeatureParameter, IEquatable<GameFeatureParameter<T>>
    {
        [SerializeField] protected T m_Value;
        
        /// <summary>
        ///   <summary> 参数值 </summary>
        /// </summary>
        public virtual T Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public GameFeatureParameter(T value = default)
        {
            m_Value = value;
        }
        
        public override void SetValue(IGameFeatureParameter parameter)
        {
            m_Value = parameter.GetValue<T>();
        }
        
        public override void ApplyOverride(IGameFeatureParameter other)
        {
            if (other is GameFeatureParameter<T> typedOther && typedOther.Overrides)
            {
                m_Value = typedOther.m_Value;
            }
        }
        
        public bool Equals(GameFeatureParameter<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GameFeatureParameter<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return EqualityComparer<T>.Default.GetHashCode(m_Value);
            }
        }
        
        public static implicit operator T(GameFeatureParameter<T> parameter) => parameter.Value;
    }
    
    
    /// <summary>
    ///   <para>浮点数范围限制参数</para>
    ///   <para>参数值会自动限制在指定范围</para>
    /// </summary>
    [Serializable]
    public class ClampedFloatParameter : GameFeatureParameter<float>
    {
        [NonSerialized] public readonly float minValue;
        [NonSerialized] public readonly float maxValue;
        
        public override float Value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, minValue, maxValue);
        }

        public ClampedFloatParameter(float value, float min, float max) 
            : base(value)
        {
            minValue = min;
            maxValue = max;
        }
    }
    
    
    /// <summary>
    ///   <para>整数范围限制参数</para>
    ///   <para>参数值会自动限制在指定范围</para>
    /// </summary>
    [Serializable]
    public class ClampedIntParameter : GameFeatureParameter<int>
    {
        [NonSerialized] public readonly int minValue;
        [NonSerialized] public readonly int maxValue;
        
        public override int Value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, minValue, maxValue);
        }

        public ClampedIntParameter(int value, int min, int max) 
            : base(value)
        {
            minValue = min;
            maxValue = max;
        }
    }

    
    /// <summary>
    ///   <para>路径参数</para>
    ///   <para>自动添加路径前缀</para>
    /// </summary>
    [Serializable]
    public class PathParameter : GameFeatureParameter<string>
    {
        /// <summary>
        ///   <para>路径前缀</para>
        /// </summary>
        public string prefix = "";
        
        public override string Value
        {
            get
            {
                if (!m_Value.StartsWith(prefix))
                {
                    return System.IO.Path.Combine(prefix, m_Value).Replace("\\", "/");
                }
                return m_Value;
            }
            set
            {
                if (value.StartsWith(prefix))
                {
                    m_Value = value.Replace("\\", "/");
                }
                else
                {
                    m_Value = System.IO.Path.Combine(prefix, value).Replace("\\", "/");
                }
            }
        }

        public PathParameter(string defaultPath = "") 
            : base(defaultPath)
        {

        }
    }
}

#endif