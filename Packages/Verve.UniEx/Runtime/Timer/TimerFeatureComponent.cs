#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Timer
{
    using Verve;
    using System;
    using Verve.Timer;
    using UnityEngine;
    
    
    /// <summary>
    /// 计时器功能组件
    /// </summary>
    public partial class TimerFeatureComponent : GameFeatureComponent
    {
        [FeatureDependency] private Verve.Timer.TimerFeature m_TimerFeature;

        
        private void Update()
        {
            m_TimerFeature?.OnUpdate(Time.deltaTime);
        }
    }
}

#endif