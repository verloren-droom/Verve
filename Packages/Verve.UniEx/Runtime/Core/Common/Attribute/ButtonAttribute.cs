#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    /// 按钮属性，用于在 Inspector 中显示为按钮，并且可以指定按钮的标签和参数。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string Label { get; }
        public string[] Args { get; }
    
        public ButtonAttribute() : this(null, null) { }
    
        public ButtonAttribute(string label) : this(label, null) { }
    
        public ButtonAttribute(string label, params string[] args)
        {
            Label = label;
            Args = args;
        }
    }
}

#endif