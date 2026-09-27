#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    ///   <para>UI 模块。</para>
    /// </summary>
    sealed partial class UIModule
    {
        /// <summary>
        ///   <para>设置 UI 根节点。</para>
        /// </summary>
        /// <param name="root">包含 <see cref="UnityEngine.Canvas"/> 的 <see cref="UnityEngine.RectTransform"/>；传入 null 使用模块自动创建的根节点。</param>
        public void SetViewRoot(Transform root)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(SetViewRoot)}");
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(UIModule));
            }

            if (root == m_ViewRoot)
            {
                return;
            }

            if (m_ViewRootLocked || m_CreatingTypes.Count > 0 || OpeningViewCount > 0 ||
                (m_ModalLayer != null && m_ModalLayer.IsVisible))
            {
                throw new InvalidOperationException(
                    $"{nameof(UIModule)} 已开始使用 UI，不能再修改 {nameof(ViewRoot)}。");
            }

            ValidateViewRoot(root);
            if (root != null && m_OwnsViewRoot && m_ViewRoot != null &&
                UICompositionPolicy.IsOwnedNode(m_ViewRoot, root))
            {
                throw new ArgumentException(
                    $"{nameof(ViewRoot)} 不能是模块自动根节点自身或其子节点。",
                    nameof(root));
            }

            if (root != null && IsOwnedLayerRoot(root))
            {
                throw new ArgumentException(
                    $"{nameof(ViewRoot)} 不能指向模块已经创建的显示层节点。",
                    nameof(root));
            }

            DestroyOwnedHierarchy();
            if (m_OwnsViewRoot && m_ViewRoot != null)
            {
                UnityEngine.Object.Destroy(m_ViewRoot.gameObject);
            }

            m_ViewRoot = root;
            m_OwnsViewRoot = false;
            if (IsInstalled && m_ViewRoot == null)
            {
                EnsureRoot();
            }
        }

        /// <summary>
        ///   <para>校验页面根节点。</para>
        /// </summary>
        /// <param name="root">根节点。</param>
        private static void ValidateViewRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            if (!(root is RectTransform))
            {
                throw new ArgumentException(
                    $"{nameof(ViewRoot)} 必须是 {nameof(RectTransform)}。",
                    nameof(root));
            }

            if (root.GetComponentInParent<Canvas>(true) == null)
            {
                throw new ArgumentException(
                    $"{nameof(ViewRoot)} 自身或父级必须包含 {nameof(Canvas)}。",
                    nameof(root));
            }
        }

        /// <summary>
        ///   <para>未指定父节点时取得对应显示层根节点。</para>
        /// </summary>
        /// <param name="parent">父级。</param>
        /// <param name="layer">层级。</param>
        private Transform GetViewParent(Transform parent, UILayer layer) => parent != null ? parent : GetLayerRoot(layer);

        /// <summary>
        ///   <para>获取层级根节点。</para>
        /// </summary>
        /// <param name="layer">层级。</param>
        private Transform GetLayerRoot(UILayer layer)
        {
            EnsureRoot();
            if (m_LayerRoots.TryGetValue(layer, out var layerRoot) && layerRoot != null)
            {
                return layerRoot;
            }

            var layerObject = new GameObject($"{nameof(UILayer)}-{layer}", typeof(RectTransform));
            layerRoot = layerObject.transform;
            layerRoot.SetParent(m_ViewRoot, false);
            var rectTransform = layerRoot as RectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            layerRoot.SetSiblingIndex(GetLayerSiblingIndex(layer));
            m_LayerRoots[layer] = layerRoot;
            return layerRoot;
        }

        /// <summary>
        ///   <para>获取层级同级索引。</para>
        /// </summary>
        /// <param name="layer">层级。</param>
        private int GetLayerSiblingIndex(UILayer layer)
        {
            var siblingIndex = 0;
            foreach (var pair in m_LayerRoots)
            {
                if (pair.Value != null && (byte)pair.Key < (byte)layer)
                {
                    siblingIndex++;
                }
            }

            return siblingIndex;
        }

        /// <summary>
        ///   <para>创建尚未初始化的根节点。</para>
        /// </summary>
        private void EnsureRoot()
        {
            if (m_ViewRoot != null)
            {
                ValidateViewRoot(m_ViewRoot);
                return;
            }

            var root = new GameObject(
                "UIRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(
                Mathf.Max(1f, m_ReferenceResolution.x),
                Mathf.Max(1f, m_ReferenceResolution.y));
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Mathf.Clamp01(m_MatchWidthOrHeight);
            m_ViewRoot = root.transform;
            GameObject.DontDestroyOnLoad(m_ViewRoot);
            m_OwnsViewRoot = true;
        }

        /// <summary>
        ///   <para>置于同级最前。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        private static void BringToFront(UIViewComponent component)
        {
            if (component != null && component.transform != null)
            {
                component.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        ///   <para>销毁拥有的层级。</para>
        /// </summary>
        private void DestroyOwnedHierarchy()
        {
            foreach (var layerRoot in m_LayerRoots.Values)
            {
                if (layerRoot != null)
                {
                    UnityEngine.Object.Destroy(layerRoot.gameObject);
                }
            }

            m_LayerRoots.Clear();
        }

        /// <summary>
        ///   <para>判断是否为拥有的层级根节点。</para>
        /// </summary>
        /// <param name="root">根节点。</param>
        private bool IsOwnedLayerRoot(Transform root)
        {
            foreach (var layerRoot in m_LayerRoots.Values)
            {
                if (layerRoot != null && UICompositionPolicy.IsOwnedNode(layerRoot, root))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
