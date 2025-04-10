namespace Verve.Input
{
    using Unit;
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 输入单元
    /// </summary>
    [CustomUnit("Input")]
    public sealed partial class InputUnit : UnitBase 
    {
        private readonly Dictionary<Type, IInputHandle> m_Inputs = new Dictionary<Type, IInputHandle>();

        public override void Startup(UnitRules parent, params object[] args)
        {
            base.Startup(parent, args);
        }
    }
}