#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Timer
{
    using Verve.Unit;
    using UnityEngine;


    [CustomUnit("Timer"), System.Serializable]
    public class TimerUnit : Verve.Timer.TimerUnit
    {
        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
        }
    }
    
    
    internal class TimerRunner : ComponentInstanceBase<TimerRunner>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}

#endif