#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using System;
    
    
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : Attribute
    {
        public string Label { get; }

        public ButtonAttribute() { }
    
        public ButtonAttribute(string label)
        {
            Label = label;
        }
    }

}

#endif