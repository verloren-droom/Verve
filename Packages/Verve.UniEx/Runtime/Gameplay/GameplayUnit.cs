#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using AI;
    using Input;
    using System;
    using Verve.Unit;
    using UnityEngine;
    using System.Collections.Generic;


    /// <summary>
    /// Gameplay 单元 
    /// </summary>
    [CustomUnit("Gameplay", 0, typeof(InputUnit), typeof(AIUnit)), Serializable]
    public partial class GameplayUnit : UnitBase
    {
        private InputUnit m_InputUnit;
        private AIUnit m_AIUnit;
        

        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
        }

        protected override void OnPostStartup(UnitRules parent)
        {
            base.OnPostStartup(parent);
            
            parent.TryGetDependency(out m_InputUnit);
            parent.TryGetDependency(out m_AIUnit);
        }
    }
}

#endif