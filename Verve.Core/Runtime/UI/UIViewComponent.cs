#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>页面绑定组件；连接预制体与页面实例。</para>
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Verve/UI View")]
    public sealed class UIViewComponent : MonoBehaviour
    {
#if UNITY_EDITOR
        /// <summary>
        ///   <para>页面类型名称。</para>
        /// </summary>
        [SerializeField, HideInInspector] private string m_ViewTypeName;
#endif
        /// <summary>
        ///   <para>变量。</para>
        /// </summary>
        [SerializeField, HideInInspector] private ViewVariable[] m_Variables = Array.Empty<ViewVariable>();
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
        ///   <para>页面。</para>
        /// </summary>
        [NonSerialized] private ViewBase m_View;

        /// <summary>
        ///   <para>组件销毁通知，仅由 <see cref="ViewBase"/> 生命周期使用。</para>
        /// </summary>
        internal event Action<UIViewComponent> Destroyed;

        /// <summary>
        ///   <para>返回生成代码使用的变量签名。</para>
        /// </summary>
        internal int VariableSignature => m_VariableSignature;

#if UNITY_EDITOR
        /// <summary>
        ///   <para>返回当前运行时绑定的 <see cref="ViewBase"/>；编辑器仅用于只读诊断。</para>
        /// </summary>
        internal ViewBase RuntimeView => m_View;
#endif

        /// <summary>
        ///   <para>由 <see cref="ViewBase"/> 绑定运行时实例。</para>
        /// </summary>
        /// <param name="view">待绑定的 <see cref="ViewBase"/>。</param>
        internal void AttachView(ViewBase view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (m_View != null && !ReferenceEquals(m_View, view))
            {
                throw new InvalidOperationException(
                    $"{nameof(UIViewComponent)} 已绑定 View {m_View.GetType().FullName}，不能重复绑定。");
            }

            UICompositionPolicy.ValidateView(this);
            ValidateViewBindings();
            m_View = view;
        }

        /// <summary>
        ///   <para>拒绝 <see cref="ViewBase"/> 引用 <see cref="WidgetBase"/> 根节点之外的内部对象。</para>
        /// </summary>
        private void ValidateViewBindings()
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
                        $"{nameof(UIViewComponent)} 的第 {i + 1} 个变量：{error}");
                }
            }
        }

        /// <summary>
        ///   <para>由 <see cref="ViewBase"/> 解除运行时实例绑定。</para>
        /// </summary>
        /// <param name="view">待解除的 <see cref="ViewBase"/>。</param>
        internal void DetachView(ViewBase view)
        {
            if (ReferenceEquals(m_View, view))
            {
                m_View = null;
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
                    $"UI 变量索引 {index} 在 {name} 上无效，请重新生成变量代码。");
            }

            return UIVariableReader.Read<T>(variables[index].Value, index, name, "UI");
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
                m_View = null;
            }
        }

        /// <summary>
        ///   <para>页面变量；保存 Unity 对象引用与事件标识。</para>
        /// </summary>
        [Serializable]
        private struct ViewVariable
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
#if UNITY_EDITOR
            /// <summary>
            ///   <para>事件标识列表。</para>
            /// </summary>
            internal string[] EventIds => m_EventIds;
#endif
        }
    }

}

#endif