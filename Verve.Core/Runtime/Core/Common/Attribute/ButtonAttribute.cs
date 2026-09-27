#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    
    /// <summary>
    ///   <para>按钮特性；在检视面板显示方法按钮。</para>
    /// </summary>
    /// <example>
    /// <code>
    /// public string arg1; // 参数1
    /// public int arg2;    // 参数2
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
        ///   <para>按钮标签。</para>
        /// </summary>
        public string Label { get; }
        /// <summary>
        ///   <para>按钮参数。</para>
        /// </summary>
        public string[] Args { get; }
    
        /// <summary>
        ///   <para>创建按钮特性。</para>
        /// </summary>
        public ButtonAttribute() : this(null, null) { }
    
        /// <summary>
        ///   <para>创建按钮特性。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        public ButtonAttribute(string label) : this(label, null) { }
    
        /// <summary>
        ///   <para>创建按钮特性。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="args">方法参数。</param>
        public ButtonAttribute(string label, params string[] args)
        {
            Label = label;
            Args = args;
        }
    }
}

#endif