namespace Verve.MVC
{
    using Unit;
    using System;
    using System.Collections.Generic;
    

    [CustomUnit("MVC")]
    public sealed partial class MVCUnit : UnitBase
    {
        private Dictionary<Type, IActivity> m_Activities = new Dictionary<Type, IActivity>();
    }
}