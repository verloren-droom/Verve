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
        private Verve.Timer.TimerFeature m_TimerFeature;

        
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            m_TimerFeature = dependencies.Get<Verve.Timer.TimerFeature>();
        }

        private void Update()
        {
            m_TimerFeature?.OnUpdate(Time.deltaTime);
        }
    }
}

#endif