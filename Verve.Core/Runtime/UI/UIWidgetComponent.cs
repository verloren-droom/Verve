#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>部件绑定组件；连接预制体与部件实例。</para>
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Verve/UI Widget")]
    public sealed class UIWidgetComponent : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>
        ///   <para>部件类型名称。</para>
        /// </summary>
        [SerializeField, HideInInspector] private string m_WidgetTypeName;
#endif
        /// <summary>
        ///   <para>变量。</para>
        /// </summary>
        [SerializeField, HideInInspector] private WidgetVariable[] m_Variables = Array.Empty<WidgetVariable>();
        /// <summary>
        ///   <para>变量签名。</para>
        /// </summary>
        [SerializeField, HideInInspector] private int m_VariableSignature;
#if UNITY_EDITOR
        /// <summary>
        ///   <para>事件签名。</para>
        /// </summary>
        [SerializeField, HideInInspector] private int m_EventSignature;
#endif
        /// <summary>
        ///   <para>部件。</para>
        /// </summary>
        [NonSerialized] private WidgetBase m_Widget;

        /// <summary>
        ///   <para>组件销毁通知，仅由 <see cref="WidgetBase"/> 生命周期使用。</para>
        /// </summary>
        internal event Action<UIWidgetComponent> Destroyed;

        /// <summary>
        ///   <para>返回生成代码使用的变量签名。</para>
        /// </summary>
        internal int VariableSignature => m_VariableSignature;

        /// <summary>
        ///   <para>返回当前运行时绑定的 <see cref="WidgetBase"/>；由 <see cref="ViewBase"/> 和 <see cref="ViewPartBase"/> 用于复用查询。</para>
        /// </summary>
        internal WidgetBase RuntimeWidget => m_Widget;

        /// <summary>
        ///   <para>由 <see cref="WidgetBase"/> 绑定运行时实例。</para>
        /// </summary>
        /// <param name="widget">待绑定的 <see cref="WidgetBase"/>。</param>
        internal void AttachWidget(WidgetBase widget)
        {
            if (widget == null)
            {
                throw new ArgumentNullException(nameof(widget));
            }

            if (m_Widget != null && !ReferenceEquals(m_Widget, widget))
            {
                throw new InvalidOperationException(
                    $"{nameof(UIWidgetComponent)} 已绑定 Widget {m_Widget.GetType().FullName}，不能重复绑定。");
            }

            UICompositionPolicy.ValidateWidget(this);
            ValidateWidgetBindings();
            m_Widget = widget;
        }

        /// <summary>
        ///   <para>拒绝 <see cref="WidgetBase"/> 引用另一个 <see cref="WidgetBase"/> 内部的节点。</para>
        /// </summary>
        private void ValidateWidgetBindings()
        {
            var variables = m_Variables;
            if (variables == null)
            {
                return;
            }

            for (var i = 0; i < variables.Length; i++)
            {
                var value = variables[i].Value;
                var node = UICompositionPolicy.GetReferenceNode(value);
                if (node == null)
                {
                    continue;
                }

                var error = UICompositionPolicy.GetBindingError(this, node);
                if (error != null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(UIWidgetComponent)} 的第 {i + 1} 个变量：{error}");
                }
            }
        }

        /// <summary>
        ///   <para>由 <see cref="WidgetBase"/> 解除运行时实例绑定。</para>
        /// </summary>
        /// <param name="widget">待解除的 <see cref="WidgetBase"/>。</param>
        internal void DetachWidget(WidgetBase widget)
        {
            if (ReferenceEquals(m_Widget, widget))
            {
                m_Widget = null;
            }
        }

        /// <summary>
        ///   <para>获取绑定的 Unity 对象变量。</para>
        /// </summary>
        /// <param name="index">变量索引。</param>
        /// <typeparam name="T">Unity 对象类型。</typeparam>
        internal T GetVariable<T>(int index) where T : UnityEngine.Object
        {
            var variables = m_Variables;
            if (variables == null || (uint)index >= (uint)variables.Length)
            {
                throw new IndexOutOfRangeException(
                    $"UI Widget 变量索引 {index} 在 {name} 上无效，请重新生成变量代码。");
            }

            return UIVariableReader.Read<T>(variables[index].Value, index, name, "UI Widget");
        }

        /// <summary>
        ///   <para>销毁时清理。</para>
        /// </summary>
        private void OnDestroy()
        {
            var callback = Destroyed;
            Destroyed = null;
            try
            {
                callback?.Invoke(this);
            }
            finally
            {
                m_Widget = null;
            }
        }

        /// <summary>
        ///   <para>部件变量；保存 Unity 对象引用与事件标识。</para>
        /// </summary>
        [Serializable]
        private struct WidgetVariable
        {
#if UNITY_EDITOR
            /// <summary>
            ///   <para>名称。</para>
            /// </summary>
            [SerializeField] private string m_Name;
#endif
            /// <summary>
            ///   <para>值。</para>
            /// </summary>
            [SerializeField] private UnityEngine.Object m_Value;
#if UNITY_EDITOR
            /// <summary>
            ///   <para>事件标识列表。</para>
            /// </summary>
            [SerializeField] private string[] m_EventIds;
#endif

            /// <summary>
            ///   <para>值。</para>
            /// </summary>
            internal UnityEngine.Object Value => m_Value;
        }
    }
}

#endif