#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    [Serializable]
    public abstract class GameFeatureParameter : IGameFeatureParameter
    {
        [NonSerialized] protected internal bool m_Overrides;
        
        /// <summary> 是否重写 </summary>
        public bool Overrides
        {
            get => m_Overrides;
            set => m_Overrides = value;
        }
        
        /// <summary> 获取值 </summary>
        public T GetValue<T>() => ((GameFeatureParameter<T>)this).Value;

        /// <summary> 设置值 </summary>
        public abstract void SetValue(IGameFeatureParameter parameter);
        /// <summary> 应用重写参数值 </summary>
        public abstract void ApplyOverride(IGameFeatureParameter other);
        
        /// <summary> 参数生命周期 </summary>
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void Release() { }
    }

    
    [Serializable]
    public class GameFeatureParameter<T> : GameFeatureParameter, IEquatable<GameFeatureParameter<T>>
    {
        [SerializeField, HideInInspector] protected T m_Value;
        
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
        
        public static implicit operator T(GameFeatureParameter<T> parameter) => parameter.m_Value;
    }
    
    
    [Serializable]
    public class ClampedFloatParameter : GameFeatureParameter<float>
    {
        [NonSerialized] public float maxValue;
        [NonSerialized] public float minValue;
        
        public override float Value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, minValue, maxValue);
        }

        public ClampedFloatParameter(float value, float min, float max, bool overrideState = false) 
            : base(value)
        {
            minValue = min;
            maxValue = max;
        }
    }
}

#endif