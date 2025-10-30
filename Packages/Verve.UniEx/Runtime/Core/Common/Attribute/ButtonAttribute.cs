#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>按钮属性</para>
    ///   <para>用于在Inspector窗口显示按钮，并且可以指定按钮的标签和参数</para>
    /// </summary>
    /// <example>
    /// <code>
    /// public string arg1; // 参数1
    /// public int arg2;    // 参数2
    /// 
    /// [Button("Do", nameof(arg1), nameof(arg2))]
    /// public void DoSomethingWithArgs(string arg1, int arg2)
    /// {
    ///     Debug.Log("Doing something with args: " + arg1 + " " + arg2);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        /// <summary>
        ///   <para>按钮标签</para>
        /// </summary>
        public string Label { get; }
        /// <summary>
        ///   <para>按钮参数</para>
        /// </summary>
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