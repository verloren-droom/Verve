#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///   <para>模态遮罩；阻断遮罩下方的 UI 输入。</para>
    /// </summary>
    public sealed class UIModalLayer
    {
        /// <summary>
        ///   <para>默认颜色。</para>
        /// </summary>
        private static readonly Color s_DefaultColor = new(0f, 0f, 0f, 0.55f);

        /// <summary>
        ///   <para>父节点获取委托。</para>
        /// </summary>
        private Func<Transform> m_ParentFactory;
        /// <summary>
        ///   <para>遮罩节点。</para>
        /// </summary>
        private GameObject m_GameObject;
        /// <summary>
        ///   <para>遮罩图像组件。</para>
        /// </summary>
        private Image m_Image;
        /// <summary>
        ///   <para>颜色。</para>
        /// </summary>
        private Color m_Color = s_DefaultColor;
        /// <summary>
        ///   <para>遮罩引用次数。</para>
        /// </summary>
        private int m_ShowCount;
        /// <summary>
        ///   <para>是否已释放。</para>
        /// </summary>
        private bool m_IsReleased;
        
        /// <summary>
        ///   <para>创建模态遮罩。</para>
        /// </summary>
        /// <param name="parentFactory">父节点获取委托。</param>
        internal UIModalLayer(Func<Transform> parentFactory) => m_ParentFactory = parentFactory ?? throw new ArgumentNullException(nameof(parentFactory));

        /// <summary>
        ///   <para>返回当前是否正在显示遮罩。</para>
        /// </summary>
        public bool IsVisible => !m_IsReleased && m_ShowCount > 0 && m_GameObject != null && m_GameObject.activeSelf;

        /// <summary>
        ///   <para>返回或设置当前遮罩颜色；透明度为 0 时仍会阻断输入。</para>
        /// </summary>
        public Color Color
        {
            get => m_Color;
            set
            {
                EnsureActive();
                m_Color = value;
                if (m_Image != null)
                {
                    m_Image.color = value;
                }
            }
        }

        /// <summary>
        ///   <para>显示遮罩并增加一层输入阻断引用。</para>
        /// </summary>
        public void Show()
        {
            EnsureActive();
            EnsureObject();
            m_ShowCount++;
            m_GameObject.transform.SetAsLastSibling();
            m_GameObject.SetActive(true);
        }

        /// <summary>
        ///   <para>减少一层输入阻断引用；引用归零后隐藏遮罩。</para>
        /// </summary>
        public void Hide()
        {
            EnsureActive();
            if (m_ShowCount == 0)
            {
                return;
            }

            m_ShowCount--;
            if (m_ShowCount == 0)
            {
                m_GameObject?.SetActive(false);
            }
        }

        /// <summary>
        ///   <para>隐藏遮罩并清空所有输入阻断引用。</para>
        /// </summary>
        public void HideAll()
        {
            EnsureActive();
            m_ShowCount = 0;
            if (m_GameObject != null)
            {
                m_GameObject.SetActive(false);
            }
        }

        /// <summary>
        ///   <para>释放遮罩对象；模块卸载时由 <see cref="UIModule"/> 调用。</para>
        /// </summary>
        internal void Release()
        {
            if (m_IsReleased)
            {
                return;
            }

            m_IsReleased = true;
            m_ShowCount = 0;
            m_ParentFactory = null;
            if (m_GameObject != null)
            {
                UnityEngine.Object.Destroy(m_GameObject);
            }

            m_GameObject = null;
            m_Image = null;
        }

        /// <summary>
        ///   <para>创建尚未初始化的对象。</para>
        /// </summary>
        private void EnsureObject()
        {
            var parent = m_ParentFactory();
            if (parent == null)
            {
                throw new InvalidOperationException("无法创建 UI 遮罩：Overlay 层根节点不存在。");
            }

            if (m_GameObject != null)
            {
                if (m_GameObject.transform.parent == parent)
                {
                    return;
                }

                UnityEngine.Object.Destroy(m_GameObject);
                m_GameObject = null;
            }

            m_ShowCount = 0;
            m_Image = null;

            var gameObject = new GameObject(nameof(UIModalLayer), typeof(RectTransform), typeof(Image));
            var rectTransform = gameObject.transform as RectTransform;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            m_Image = gameObject.GetComponent<Image>();
            m_Image.color = m_Color;
            m_Image.raycastTarget = true;
            rectTransform.SetAsLastSibling();
            m_GameObject = gameObject;
        }

        /// <summary>
        ///   <para>检查对象处于活动状态。</para>
        /// </summary>
        private void EnsureActive()
        {
            if (m_IsReleased)
            {
                throw new ObjectDisposedException(nameof(UIModalLayer));
            }

            Game.ThrowIfNotOnMainThread(nameof(UIModalLayer));
        }
    }
}

#endif