#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>UI 组合规则；约束页面、部件及节点的归属。</para>
    /// </summary>
    internal static class UICompositionPolicy
    {
        /// <summary>
        ///   <para>返回引用对象对应的节点；非节点资源返回 null。</para>
        /// </summary>
        /// <param name="value">值。</param>
        internal static GameObject GetReferenceNode(UnityEngine.Object value) => value as GameObject ?? (value as Component)?.gameObject;

        /// <summary>
        ///   <para>返回节点自身或父级最近的 <see cref="WidgetBase"/> 组件。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        internal static UIWidgetComponent FindWidgetOwner(GameObject node) => node != null ? node.GetComponentInParent<UIWidgetComponent>(true) : null;

        /// <summary>
        ///   <para>返回跨越 View/Widget 边界的绑定错误；合法目标返回 null。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        internal static string GetBindingError(Component owner, GameObject node)
        {
            if (owner == null || node == null)
            {
                return null;
            }
            
            if (owner.gameObject.scene.IsValid() && node.scene.IsValid() &&
                !IsOwnedNode(owner, node))
            {
                return "UI 变量必须引用当前 UI 节点树中的对象。";
            }

            var widget = FindWidgetOwner(node);
            if (owner is UIViewComponent)
            {
                return widget == null || widget.gameObject == node
                    ? null
                    : $"View 不能绑定 Widget“{widget.name}”内部的对象。";
            }

            if (owner is UIWidgetComponent widgetOwner &&
                widget != widgetOwner &&
                widget != null &&
                widget.gameObject != node)
            {
                return $"Widget 不能绑定另一个 Widget“{widget.name}”内部的对象。";
            }

            return null;
        }

        /// <summary>
        ///   <para>判断节点是否位于 UI 组件根节点下；资源对象不参与运行时层级判断。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        internal static bool IsOwnedNode(Component owner, GameObject node)
        {
            if (owner == null || owner.gameObject == null || node == null)
            {
                return false;
            }

            var ownerScene = owner.gameObject.scene;
            var nodeScene = node.scene;
            if (!ownerScene.IsValid() || !nodeScene.IsValid())
            {
                return true;
            }

            return IsOwnedNode(owner.transform, node.transform);
        }

        /// <summary>
        ///   <para>判断目标节点是否为根节点或其子节点。</para>
        /// </summary>
        /// <param name="root">根节点。</param>
        /// <param name="node">节点。</param>
        internal static bool IsOwnedNode(Transform root, Transform node)
        {
            return root != null && node != null &&
                   node.IsChildOf(root);
        }

        /// <summary>
        ///   <para>返回 <see cref="ViewBase"/> 父节点的层级错误；合法父节点返回 null。</para>
        /// </summary>
        /// <param name="parent">父级。</param>
        /// <param name="viewTransform">页面变换。</param>
        internal static string GetViewParentError(Transform parent, Transform viewTransform)
        {
            if (parent == null)
            {
                return null;
            }

            if (IsOwnedNode(viewTransform, parent))
            {
                return "View 的父节点不能是自身或其子节点。";
            }

            for (var current = parent; current != null; current = current.parent)
            {
                if (current.GetComponent<UIViewComponent>() != null)
                {
                    return "View 不能挂载在另一个 View 内。";
                }

                if (current.GetComponent<UIWidgetComponent>() != null)
                {
                    return "View 不能挂载在 Widget 内。";
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>返回组件组合冲突；合法组合返回 null。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        internal static string GetCompositionError(Component owner)
        {
            if (owner == null || owner.gameObject == null)
            {
                return null;
            }

            var root = owner.gameObject;
            var view = root.GetComponent<UIViewComponent>();
            var widget = root.GetComponent<UIWidgetComponent>();
            if (view != null && widget != null)
            {
                return "同一节点不能同时挂载 UIViewComponent 和 UIWidgetComponent。";
            }

            if (owner is UIViewComponent)
            {
                for (var parent = root.transform.parent; parent != null; parent = parent.parent)
                {
                    if (parent.GetComponent<UIViewComponent>() != null)
                    {
                        return "View 内不允许嵌套 View。";
                    }

                    if (parent.GetComponent<UIWidgetComponent>() != null)
                    {
                        return "Widget 内不允许包含 View。";
                    }
                }

                return HasOtherView(root, owner)
                    ? "View 内不允许嵌套 View。"
                    : null;
            }

            return owner is UIWidgetComponent && HasOtherView(root, owner)
                ? "Widget 内不允许包含 View。"
                : null;
        }

        /// <summary>
        ///   <para>验证 <see cref="ViewBase"/> 根节点边界。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        internal static void ValidateView(UIViewComponent owner)
        {
            ValidateOwner(owner);
            ThrowIfInvalid(owner);
        }

        /// <summary>
        ///   <para>验证 <see cref="WidgetBase"/> 根节点边界。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        internal static void ValidateWidget(UIWidgetComponent owner)
        {
            ValidateOwner(owner);
            ThrowIfInvalid(owner);
        }

        /// <summary>
        ///   <para>校验所有者。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        private static void ValidateOwner(Component owner)
        {
            if (owner == null || owner.gameObject == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
        }

        /// <summary>
        ///   <para>无效时抛出异常。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        private static void ThrowIfInvalid(Component owner)
        {
            var error = GetCompositionError(owner);
            if (error != null)
            {
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        ///   <para>判断是否包含另一个页面。</para>
        /// </summary>
        /// <param name="root">根节点。</param>
        /// <param name="owner">所有者。</param>
        private static bool HasOtherView(GameObject root, Component owner)
        {
            var views = root.GetComponentsInChildren<UIViewComponent>(true);
            for (var i = 0; i < views.Length; i++)
            {
                if (views[i] != owner)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif