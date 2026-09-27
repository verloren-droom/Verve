#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditorInternal;
    using UnityEngine;

    /// <summary>
    ///   <para>UI 层级绑定工具；绘制绑定标记并提供节点绑定菜单。</para>
    /// </summary>
    [InitializeOnLoad]
    internal static class UIHierarchyBindingTools
    {
        /// <summary>
        ///   <para>标记大小。</para>
        /// </summary>
        private const float BadgeSize = 15f;
        /// <summary>
        ///   <para>标记右内边距。</para>
        /// </summary>
        private const float BadgeRightPadding = 4f;
        /// <summary>
        ///   <para>节点图标宽度。</para>
        /// </summary>
        private const float NodeIconWidth = 18f;
        /// <summary>
        ///   <para>名称标记间距。</para>
        /// </summary>
        private const float NameBadgeSpacing = 4f;
        /// <summary>
        ///   <para>紧凑标记宽度。</para>
        /// </summary>
        private const float CompactBadgeWidth = 3f;
        /// <summary>
        ///   <para>事件标记高度。</para>
        /// </summary>
        private const float EventBadgeHeight = 9f;
        /// <summary>
        ///   <para>事件标记右溢出。</para>
        /// </summary>
        private const float EventBadgeRightOverflow = 1f;
        /// <summary>
        ///   <para>紧凑事件间距。</para>
        /// </summary>
        private const float CompactEventGap = 2f;
        /// <summary>
        ///   <para>紧凑事件右内边距。</para>
        /// </summary>
        private const float CompactEventRightPadding = 1f;
        /// <summary>
        ///   <para>最大可见事件数量。</para>
        /// </summary>
        private const int MaxVisibleEventCount = 9;
        /// <summary>
        ///   <para>最大提示事件数量。</para>
        /// </summary>
        private const int MaxTooltipEventCount = 3;
        /// <summary>
        ///   <para>绑定菜单根节点。</para>
        /// </summary>
        private const string BindingMenuRoot = "Verve/绑定 UI 组件";
        /// <summary>
        ///   <para>绑定。</para>
        /// </summary>
        private static readonly Dictionary<int, NodeBinding> s_Bindings = new();
        /// <summary>
        ///   <para>左链接。</para>
        /// </summary>
        private static readonly Vector3[] s_LeftLink = new Vector3[7];
        /// <summary>
        ///   <para>右链接。</para>
        /// </summary>
        private static readonly Vector3[] s_RightLink = new Vector3[7];
        /// <summary>
        ///   <para>链接桥接。</para>
        /// </summary>
        private static readonly Vector3[] s_LinkBridge = new Vector3[2];
        /// <summary>
        ///   <para>事件数量样式。</para>
        /// </summary>
        private static GUIStyle s_EventCountStyle;
        /// <summary>
        ///   <para>脏标记。</para>
        /// </summary>
        private static bool s_Dirty = true;

        /// <summary>
        ///   <para>创建 UI 层级绑定工具。</para>
        /// </summary>
        static UIHierarchyBindingTools()
        {
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawItemByEntityId;
#else
            EditorApplication.hierarchyWindowItemOnGUI += DrawItem;
#endif
            SceneHierarchyHooks.addItemsToGameObjectContextMenu += AddBindingItemsToContextMenu;
            EditorApplication.hierarchyChanged += InvalidateBindings;
            EditorApplication.projectChanged += InvalidateBindings;
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += InvalidateBindings;
        }

#if UNITY_6000_4_OR_NEWER
        /// <summary>
        ///   <para>按实体标识绘制项。</para>
        /// </summary>
        /// <param name="entityId">实体标识。</param>
        /// <param name="selectionRect">选择区域。</param>
        private static void DrawItemByEntityId(EntityId entityId, Rect selectionRect) => DrawItem((int)entityId.GetRawData(), selectionRect);
#endif

        /// <summary>
        ///   <para>绘制项。</para>
        /// </summary>
        /// <param name="instanceId">实例标识。</param>
        /// <param name="selectionRect">选择区域。</param>
        private static void DrawItem(int instanceId, Rect selectionRect)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.Repaint)
            {
                return;
            }

            if (s_Dirty)
            {
                RebuildBindings();
            }

            if (!s_Bindings.TryGetValue(instanceId, out var binding))
            {
                return;
            }

            if (binding.BindingContent == null ||
                (binding.EventCount > 0 && binding.EventContent == null))
            {
                binding.BindingContent ??= CreateBindingContent(binding);
                if (binding.EventCount > 0)
                {
                    binding.EventContent ??= CreateEventContent(binding);
                }

                s_Bindings[instanceId] = binding;
            }

            var badgeX = selectionRect.xMax - BadgeSize - BadgeRightPadding;
            var nameRight = selectionRect.x + NodeIconWidth + binding.NameWidth;
            if (badgeX < nameRight + NameBadgeSpacing)
            {
                DrawCompactBadge(selectionRect, binding);
                return;
            }

            DrawBadge(new Rect(
                badgeX,
                selectionRect.y + (selectionRect.height - BadgeSize) * 0.5f,
                BadgeSize,
                BadgeSize), binding);
        }

        /// <summary>
        ///   <para>绘制标记。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="binding">绑定。</param>
        private static void DrawBadge(Rect rect, NodeBinding binding)
        {
            var color = GetBadgeColor(binding.flag);
            var centerY = rect.center.y;
            s_LeftLink[0] = new Vector3(rect.x + 6.5f, centerY - 3f);
            s_LeftLink[1] = new Vector3(rect.x + 4f, centerY - 3f);
            s_LeftLink[2] = new Vector3(rect.x + 2.5f, centerY - 1.5f);
            s_LeftLink[3] = new Vector3(rect.x + 2.5f, centerY + 1.5f);
            s_LeftLink[4] = new Vector3(rect.x + 4f, centerY + 3f);
            s_LeftLink[5] = new Vector3(rect.x + 6.5f, centerY + 3f);
            s_LeftLink[6] = new Vector3(rect.x + 7.5f, centerY + 2f);
            s_RightLink[0] = new Vector3(rect.x + 7.5f, centerY - 2f);
            s_RightLink[1] = new Vector3(rect.x + 8.5f, centerY - 3f);
            s_RightLink[2] = new Vector3(rect.x + 11f, centerY - 3f);
            s_RightLink[3] = new Vector3(rect.x + 12.5f, centerY - 1.5f);
            s_RightLink[4] = new Vector3(rect.x + 12.5f, centerY + 1.5f);
            s_RightLink[5] = new Vector3(rect.x + 11f, centerY + 3f);
            s_RightLink[6] = new Vector3(rect.x + 8.5f, centerY + 3f);
            s_LinkBridge[0] = new Vector3(rect.x + 5f, centerY);
            s_LinkBridge[1] = new Vector3(rect.x + 10f, centerY);

            Handles.BeginGUI();
            var previousColor = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.08f, 0.08f, 0.08f, 0.72f)
                : new Color(1f, 1f, 1f, 0.82f);
            Handles.DrawSolidDisc(rect.center, Vector3.forward, BadgeSize * 0.47f);
            Handles.color = new Color(0f, 0f, 0f, 0.7f);
            Handles.DrawAAPolyLine(3f, s_LeftLink);
            Handles.DrawAAPolyLine(3f, s_RightLink);
            Handles.DrawAAPolyLine(3f, s_LinkBridge);
            Handles.color = color;
            Handles.DrawAAPolyLine(1.5f, s_LeftLink);
            Handles.DrawAAPolyLine(1.5f, s_RightLink);
            Handles.DrawAAPolyLine(1.5f, s_LinkBridge);

            Rect eventRect = default;
            if (binding.EventContent != null)
            {
                var eventWidth = Mathf.Max(
                    EventBadgeHeight,
                    Mathf.Ceil(EventCountStyle.CalcSize(binding.EventContent).x) + 4f);
                eventRect = new Rect(
                    rect.xMax - eventWidth + EventBadgeRightOverflow,
                    rect.y - 1f,
                    eventWidth,
                    EventBadgeHeight);
                DrawCapsule(eventRect, new Color(0f, 0f, 0f, 0.8f));
                DrawCapsule(
                    new Rect(
                        eventRect.x + 1f,
                        eventRect.y + 1f,
                        eventRect.width - 2f,
                        eventRect.height - 2f),
                    new Color(1f, 0.55f, 0.08f));
            }

            Handles.color = previousColor;
            Handles.EndGUI();
            GUI.Label(rect, binding.BindingContent);
            if (binding.EventContent != null)
            {
                GUI.Label(eventRect, binding.EventContent, EventCountStyle);
            }
        }

        /// <summary>
        ///   <para>绘制紧凑标记。</para>
        /// </summary>
        /// <param name="selectionRect">选择区域。</param>
        /// <param name="binding">绑定。</param>
        private static void DrawCompactBadge(Rect selectionRect, NodeBinding binding)
        {
            var eventWidth = binding.EventContent == null
                ? 0f
                : Mathf.Max(
                    EventBadgeHeight,
                    Mathf.Ceil(EventCountStyle.CalcSize(binding.EventContent).x) + 4f);
            var eventRect = eventWidth > 0f
                ? new Rect(
                    selectionRect.xMax - eventWidth - CompactEventRightPadding,
                    selectionRect.y + (selectionRect.height - EventBadgeHeight) * 0.5f,
                    eventWidth,
                    EventBadgeHeight)
                : default;
            var rect = new Rect(
                eventWidth > 0f
                    ? eventRect.xMin - CompactEventGap - CompactBadgeWidth
                    : selectionRect.xMax - CompactBadgeWidth - 2f,
                selectionRect.y + (selectionRect.height > 4f ? 2f : 0f),
                CompactBadgeWidth,
                Mathf.Max(1f, selectionRect.height - (selectionRect.height > 4f ? 4f : 0f)));
            EditorGUI.DrawRect(
                new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f),
                new Color(0f, 0f, 0f, 0.65f));
            EditorGUI.DrawRect(rect, GetBadgeColor(binding.flag));
            if (eventWidth > 0f)
            {
                Handles.BeginGUI();
                DrawCapsule(eventRect, new Color(0f, 0f, 0f, 0.8f));
                DrawCapsule(
                    new Rect(
                        eventRect.x + 1f,
                        eventRect.y + 1f,
                        eventRect.width - 2f,
                        eventRect.height - 2f),
                    new Color(1f, 0.55f, 0.08f));
                Handles.EndGUI();
            }

            GUI.Label(
                new Rect(
                    rect.x - BadgeSize + CompactBadgeWidth,
                    selectionRect.y,
                    BadgeSize,
                    selectionRect.height),
                binding.BindingContent);
            if (eventWidth > 0f)
            {
                GUI.Label(eventRect, binding.EventContent, EventCountStyle);
            }
        }

        /// <summary>
        ///   <para>将绑定目标直接加入 Hierarchy 原生的 Verve 绑定子菜单。</para>
        /// </summary>
        /// <param name="menu">菜单。</param>
        /// <param name="node">节点。</param>
        private static void AddBindingItemsToContextMenu(GenericMenu menu, GameObject node)
        {
            if (menu == null || node == null)
            {
                return;
            }

            PopulateBindingMenu(menu, GetSelectedNodes(node), BindingMenuRoot);
        }

        /// <summary>
        ///   <para>移除节点的 UI 绑定。</para>
        /// </summary>
        /// <param name="command">命令。</param>
        [MenuItem("GameObject/Verve/移除选中对象的 UI 绑定", false, 32)]
        private static void RemoveNodeFromUI(MenuCommand command) => RemoveNodeBinding(command);

        /// <summary>
        ///   <para>判断节点是否可移除 UI 绑定。</para>
        /// </summary>
        /// <param name="command">命令。</param>
        [MenuItem("GameObject/Verve/移除选中对象的 UI 绑定", true)]
        private static bool CanRemoveNodeFromUI(MenuCommand command)
        {
            var nodes = GetSelectedNodes(CoreEditorUtility.GetCommandGameObject(command));
            if (nodes.Count == 0)
            {
                return false;
            }

            for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                if (HasRemovableBinding(nodes[nodeIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>判断是否包含可绑定组件。</para>
        /// </summary>
        /// <param name="contextNode">上下文节点。</param>
        private static bool HasBindableComponent(GameObject contextNode) => GetBindingOptions(GetSelectedNodes(contextNode)).Count > 0;

        /// <summary>
        ///   <para>判断是否包含可移除绑定。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        private static bool HasRemovableBinding(GameObject node)
        {
            var owners = GetBindingOwners(node);
            for (var i = 0; i < owners.Count; i++)
            {
                if (CanPrepareBinding(owners[i], node) && HasBoundNode(owners[i], node))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>返回 Hierarchy 当前右键上下文对应的有效选择。</para>
        /// </summary>
        /// <param name="contextNode">上下文节点。</param>
        private static List<GameObject> GetSelectedNodes(GameObject contextNode)
        {
            var selection = Selection.gameObjects;
            var nodes = new List<GameObject>(selection == null ? 1 : selection.Length);
            var contextIsSelected = contextNode == null;
            if (selection != null)
            {
                for (var i = 0; i < selection.Length; i++)
                {
                    var node = selection[i];
                    if (node == null || !IsHierarchyObject(node) || ContainsNode(nodes, node))
                    {
                        continue;
                    }

                    nodes.Add(node);
                    contextIsSelected |= node == contextNode;
                }
            }

            // 右键未选中对象时，Unity 可能不会同步 Selection；此时只处理上下文节点。
            if (contextNode != null && (!contextIsSelected || nodes.Count == 0))
            {
                nodes.Clear();
                if (IsHierarchyObject(contextNode))
                {
                    nodes.Add(contextNode);
                }
            }

            return nodes;
        }

        /// <summary>
        ///   <para>包含节点。</para>
        /// </summary>
        /// <param name="nodes">节点。</param>
        /// <param name="node">节点。</param>
        private static bool ContainsNode(List<GameObject> nodes, GameObject node)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == node)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>获取绑定选项。</para>
        /// </summary>
        /// <param name="nodes">节点。</param>
        private static List<BindingOption> GetBindingOptions(IReadOnlyList<GameObject> nodes)
        {
            var options = new List<BindingOption>();
            if (nodes == null || nodes.Count == 0)
            {
                return options;
            }

            var candidateSets = new List<List<BindingCandidate>>(nodes.Count);
            for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                candidateSets.Add(GetBindingCandidates(nodes[nodeIndex]));
            }

            var firstCandidates = candidateSets[0];
            for (var i = 0; i < firstCandidates.Count; i++)
            {
                var candidate = firstCandidates[i];
                var targets = new List<BindingTarget>(nodes.Count) { candidate.Target };
                var matchesAll = true;
                for (var nodeIndex = 1; nodeIndex < nodes.Count; nodeIndex++)
                {
                    var match = FindCandidate(candidateSets[nodeIndex], candidate.Key);
                    if (!match.HasValue)
                    {
                        matchesAll = false;
                        break;
                    }

                    targets.Add(match.Value.Target);
                }

                if (matchesAll)
                {
                    options.Add(new BindingOption(
                        candidate.Target.Owner,
                        candidate.OwnerLabel,
                        candidate.ComponentLabel,
                        targets));
                }
            }

            return options;
        }

        /// <summary>
        ///   <para>获取绑定候选。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        private static List<BindingCandidate> GetBindingCandidates(GameObject node)
        {
            var candidates = new List<BindingCandidate>();
            var owners = GetBindingOwners(node);
            for (var ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                var owner = owners[ownerIndex];
                if (HasBoundNode(owner, node))
                {
                    continue;
                }

                var components = GetBindableComponents(owner, node);
                var ownerLabel = GetOwnerMenuLabel(owner);
                var typeCounts = new Dictionary<Type, int>();
                for (var componentIndex = 0; componentIndex < components.Count; componentIndex++)
                {
                    var component = components[componentIndex];
                    var type = component.GetType();
                    typeCounts.TryGetValue(type, out var typeCount);
                    typeCount++;
                    typeCounts[type] = typeCount;
                    var typeName = UIVariablesGenerator.GetTypeName(type);
                    var componentLabel = typeCount == 1 ? typeName : $"{typeName} #{typeCount}";
                    candidates.Add(new BindingCandidate(
                        new BindingKey(ownerIndex, ownerLabel, type, typeCount),
                        ownerLabel,
                        componentLabel,
                        new BindingTarget(owner, component)));
                }
            }

            return candidates;
        }

        /// <summary>
        ///   <para>查找候选。</para>
        /// </summary>
        /// <param name="candidates">候选。</param>
        /// <param name="key">键。</param>
        private static BindingCandidate? FindCandidate(
            IReadOnlyList<BindingCandidate> candidates,
            BindingKey key)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Key.Equals(key))
                {
                    return candidates[i];
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>统计不同的绑定所有者。</para>
        /// </summary>
        /// <param name="options">选项。</param>
        private static int CountDistinctOwners(IReadOnlyList<BindingOption> options)
        {
            var owners = new HashSet<int>();
            for (var i = 0; i < options.Count; i++)
            {
                var owner = options[i].Owner;
                if (owner != null)
                {
                    owners.Add(owner.GetInstanceID());
                }
            }

            return owners.Count;
        }

        /// <summary>
        ///   <para>填充绑定菜单。</para>
        /// </summary>
        /// <param name="menu">菜单。</param>
        /// <param name="nodes">节点。</param>
        /// <param name="menuRoot">菜单根节点。</param>
        private static void PopulateBindingMenu(
            GenericMenu menu,
            IReadOnlyList<GameObject> nodes,
            string menuRoot)
        {
            if (menu == null || nodes == null || nodes.Count == 0)
            {
                return;
            }

            var options = GetBindingOptions(nodes);
            var useOwnerGroup = CountDistinctOwners(options) > 1;
            for (var optionIndex = 0; optionIndex < options.Count; optionIndex++)
            {
                var option = options[optionIndex];
                var bindingPath = useOwnerGroup
                    ? string.IsNullOrEmpty(menuRoot)
                        ? option.OwnerLabel
                        : $"{menuRoot}/{option.OwnerLabel}"
                    : menuRoot;
                var label = string.IsNullOrEmpty(bindingPath)
                    ? option.ComponentLabel
                    : $"{bindingPath}/{option.ComponentLabel}";
                var selectedOption = option;
                menu.AddItem(new GUIContent(label), false, () => AddBindings(selectedOption.Targets));
            }
        }

        /// <summary>
        ///   <para>移除节点绑定。</para>
        /// </summary>
        /// <param name="command">命令。</param>
        private static void RemoveNodeBinding(MenuCommand command)
        {
            var nodes = GetSelectedNodes(CoreEditorUtility.GetCommandGameObject(command));
            if (nodes.Count == 0)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("移除 UI 绑定");
            var removed = false;
            for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                var owners = GetBindingOwners(nodes[nodeIndex]);
                for (var ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
                {
                    removed |= RemoveBindingsInternal(owners[ownerIndex], nodes[nodeIndex]);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            if (removed)
            {
                InvalidateBindings();
                InternalEditorUtility.RepaintAllViews();
            }
        }

        /// <summary>
        ///   <para>判断是否允许准备绑定。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        private static bool CanPrepareBinding(Component owner, GameObject node)
        {
            return owner != null && node != null &&
                UIComponentEditorUtility.CanEditComponent(owner) &&
                UICompositionPolicy.GetCompositionError(owner) == null &&
                UICompositionPolicy.IsOwnedNode(owner, node);
        }

        /// <summary>
        ///   <para>返回当前节点上方所有可作为绑定宿主的 View/Widget 组件。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        private static List<Component> GetBindingOwners(GameObject node)
        {
            var owners = new List<Component>(2);
            for (var current = node != null ? node.transform : null;
                 current != null;
                 current = current.parent)
            {
                var widget = current.GetComponent<UIWidgetComponent>();
                if (widget != null)
                {
                    owners.Add(widget);
                }

                var view = current.GetComponent<UIViewComponent>();
                if (view != null)
                {
                    owners.Add(view);
                }
            }

            return owners;
        }

        /// <summary>
        ///   <para>返回节点上允许写入指定宿主的组件。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        private static List<Component> GetBindableComponents(Component owner, GameObject node)
        {
            var components = new List<Component>();
            if (!CanPrepareBinding(owner, node) ||
                UIComponentEditorUtility.GetConfiguredType(owner) == null ||
                UICompositionPolicy.GetBindingError(owner, node) != null)
            {
                return components;
            }

            var nodeComponents = node.GetComponents<Component>();
            for (var i = 0; i < nodeComponents.Length; i++)
            {
                var component = nodeComponents[i];
                if (component != null && UIComponentEditorUtility.CanAssignValue(owner, component))
                {
                    components.Add(component);
                }
            }

            return components;
        }

        /// <summary>
        ///   <para>获取所有者菜单标签。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        private static string GetOwnerMenuLabel(Component owner)
        {
            var type = UIComponentEditorUtility.GetConfiguredType(owner);
            var name = type?.FullName;
            if (string.IsNullOrEmpty(name))
            {
                name = owner is UIViewComponent ? "View" : "Widget";
            }

            return owner is UIViewComponent ? $"View：{name}" : $"Widget：{name}";
        }

        /// <summary>
        ///   <para>判断是否包含已绑定节点。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        private static bool HasBoundNode(Component owner, GameObject node)
        {
            using var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var variables = serializedObject.FindProperty(UIComponentEditorUtility.VariablesField);
            if (variables == null || !variables.isArray)
            {
                return false;
            }

            for (var i = 0; i < variables.arraySize; i++)
            {
                var value = variables
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative(UIComponentEditorUtility.VariableValueField)
                    ?.objectReferenceValue;
                if (UICompositionPolicy.GetReferenceNode(value) == node)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>添加绑定。</para>
        /// </summary>
        /// <param name="targets">目标。</param>
        private static void AddBindings(IReadOnlyList<BindingTarget> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("添加 UI 绑定");
            var added = false;
            for (var i = 0; i < targets.Count; i++)
            {
                added |= AddBindingInternal(targets[i].Owner, targets[i].Value);
            }

            Undo.CollapseUndoOperations(undoGroup);
            if (added)
            {
                InvalidateBindings();
                InternalEditorUtility.RepaintAllViews();
            }
        }

        /// <summary>
        ///   <para>添加绑定。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="component">组件。</param>
        private static bool AddBindingInternal(Component owner, Component component)
        {
            if (owner == null || component == null ||
                !UIComponentEditorUtility.CanAssignValue(owner, component) ||
                HasBoundNode(owner, component.gameObject))
            {
                return false;
            }

            using var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var variables = serializedObject.FindProperty(UIComponentEditorUtility.VariablesField);
            if (variables == null || !variables.isArray)
            {
                return false;
            }

            Undo.RecordObject(owner, "添加 UI 绑定");
            var index = variables.arraySize;
            variables.InsertArrayElementAtIndex(index);
            var element = variables.GetArrayElementAtIndex(index);
            var nameProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField);
            var valueProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField);
            var eventIdsProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField);
            if (nameProperty == null || valueProperty == null)
            {
                // 保持序列化数组完整，避免留下无法编辑的空元素。
                variables.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                return false;
            }

            var ownerType = UIComponentEditorUtility.GetConfiguredType(owner);
            nameProperty.stringValue =
                CreateUniqueVariableName(variables, component.gameObject.name, index, ownerType);
            valueProperty.objectReferenceValue = component;
            if (eventIdsProperty != null)
            {
                eventIdsProperty.arraySize = 0;
            }
            if (!serializedObject.ApplyModifiedProperties())
            {
                return false;
            }

            EditorUtility.SetDirty(owner);
            if (owner.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
            return true;
        }

        /// <summary>
        ///   <para>移除绑定。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        private static bool RemoveBindingsInternal(Component owner, GameObject node)
        {
            if (owner == null || node == null || !HasBoundNode(owner, node))
            {
                return false;
            }

            using var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var variables = serializedObject.FindProperty(UIComponentEditorUtility.VariablesField);
            if (variables == null || !variables.isArray)
            {
                return false;
            }

            var removed = false;
            Undo.RecordObject(owner, "移除 UI 绑定");
            for (var i = variables.arraySize - 1; i >= 0; i--)
            {
                var element = variables.GetArrayElementAtIndex(i);
                var value = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField)
                    ?.objectReferenceValue;
                if (UICompositionPolicy.GetReferenceNode(value) != node)
                {
                    continue;
                }

                var size = variables.arraySize;
                variables.DeleteArrayElementAtIndex(i);
                // Unity 对包含 Object 引用的序列化元素可能第一次只清空引用。
                if (variables.arraySize == size && i < variables.arraySize)
                {
                    variables.DeleteArrayElementAtIndex(i);
                }

                removed = variables.arraySize != size || removed;
            }

            if (!removed || !serializedObject.ApplyModifiedProperties())
            {
                return false;
            }

            EditorUtility.SetDirty(owner);
            if (owner.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
            return true;
        }

        /// <summary>
        ///   <para>创建唯一变量名称。</para>
        /// </summary>
        /// <param name="variables">变量。</param>
        /// <param name="sourceName">源名称。</param>
        /// <param name="ignoredIndex">忽略的索引。</param>
        /// <param name="ownerType">所有者类型。</param>
        private static string CreateUniqueVariableName(
            SerializedProperty variables,
            string sourceName,
            int ignoredIndex,
            Type ownerType)
        {
            var baseName = UIVariablesGenerator.CreateIdentifier(sourceName);
            var name = baseName;
            var suffix = 2;
            while (HasVariableName(variables, name, ignoredIndex) ||
                   UIComponentEditorUtility.HasMemberConflict(ownerType, name))
            {
                name = $"{baseName}{suffix++}";
            }

            return name;
        }

        /// <summary>
        ///   <para>判断是否包含变量名称。</para>
        /// </summary>
        /// <param name="variables">变量。</param>
        /// <param name="name">名称。</param>
        /// <param name="ignoredIndex">忽略的索引。</param>
        private static bool HasVariableName(
            SerializedProperty variables,
            string name,
            int ignoredIndex)
        {
            for (var i = 0; i < variables.arraySize; i++)
            {
                if (i == ignoredIndex)
                {
                    continue;
                }

                var currentName = variables
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative(UIComponentEditorUtility.VariableNameField)
                    ?.stringValue;
                if (string.Equals(currentName, name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>查找最近 UI 组件。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="flag">标记。</param>
        private static Component FindNearestUIComponent(GameObject node, BindingFlag flag)
        {
            for (var current = node != null ? node.transform : null;
                 current != null;
                 current = current.parent)
            {
                if (flag == BindingFlag.View)
                {
                    var view = current.GetComponent<UIViewComponent>();
                    if (view != null)
                    {
                        return view;
                    }

                    continue;
                }

                if (flag == BindingFlag.Widget)
                {
                    var widget = current.GetComponent<UIWidgetComponent>();
                    if (widget != null)
                    {
                        return widget;
                    }

                    continue;
                }

                var viewComponent = current.GetComponent<UIViewComponent>();
                if (viewComponent != null)
                {
                    return viewComponent;
                }

                var widgetComponent = current.GetComponent<UIWidgetComponent>();
                if (widgetComponent != null)
                {
                    return widgetComponent;
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>复制当前 View/Widget 子节点相对路径。</para>
        /// </summary>
        /// <param name="command">命令。</param>
        [MenuItem("GameObject/Verve/复制 UI 子节点路径", false, 30)]
        private static void CopyRelativePath(MenuCommand command)
        {
            var node = CoreEditorUtility.GetCommandGameObject(command);
            if (!TryGetBindingRoot(node, out var root) || root == node.transform)
            {
                return;
            }

            var path = AnimationUtility.CalculateTransformPath(node.transform, root);
            EditorGUIUtility.systemCopyBuffer = path;
            EditorWindow.focusedWindow?.ShowNotification(new GUIContent($"已复制路径：{path}"));
        }

        /// <summary>
        ///   <para>判断是否允许复制相对路径。</para>
        /// </summary>
        /// <param name="command">命令。</param>
        [MenuItem("GameObject/Verve/复制 UI 子节点路径", true)]
        private static bool CanCopyRelativePath(MenuCommand command)
        {
            var node = CoreEditorUtility.GetCommandGameObject(command);
            return TryGetBindingRoot(node, out var root) && root != node.transform;
        }

        /// <summary>
        ///   <para>尝试获取绑定根节点。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="root">根节点。</param>
        private static bool TryGetBindingRoot(GameObject node, out Transform root)
        {
            root = FindNearestUIComponent(node, BindingFlag.None)?.transform;
            return root != null;
        }

        /// <summary>
        ///   <para>绘制胶囊。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="color">颜色。</param>
        private static void DrawCapsule(Rect rect, Color color)
        {
            var radius = rect.height * 0.5f;
            var centerY = rect.center.y;
            var left = rect.x + radius;
            var right = rect.xMax - radius;
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(left, centerY), Vector3.forward, radius);
            Handles.DrawSolidDisc(new Vector3(right, centerY), Vector3.forward, radius);
            if (right > left)
            {
                EditorGUI.DrawRect(new Rect(left, rect.y, right - left, rect.height), color);
            }
        }

        /// <summary>
        ///   <para>重建绑定。</para>
        /// </summary>
        private static void RebuildBindings()
        {
            s_Bindings.Clear();
            IndexBindings<UIViewComponent>(BindingFlag.View);
            IndexBindings<UIWidgetComponent>(BindingFlag.Widget);
            s_Dirty = false;
        }

        /// <summary>
        ///   <para>索引绑定。</para>
        /// </summary>
        /// <param name="flag">标记。</param>
        /// <typeparam name="TOwner">所有者类型。</typeparam>
        private static void IndexBindings<TOwner>(BindingFlag flag) where TOwner : Component
        {
            var owners = Resources.FindObjectsOfTypeAll<TOwner>();
            for (var i = 0; i < owners.Length; i++)
            {
                if (IsHierarchyObject(owners[i]))
                {
                    IndexOwnerBindings(owners[i], flag);
                }
            }
        }

        /// <summary>
        ///   <para>判断是否为层级对象。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        private static bool IsHierarchyObject(Component component)
        {
            if (component == null || !component.gameObject.scene.IsValid())
            {
                return false;
            }

            return !EditorSceneManager.IsPreviewScene(component.gameObject.scene) ||
                   PrefabStageUtility.GetPrefabStage(component.gameObject) != null;
        }

        /// <summary>
        ///   <para>判断是否为层级对象。</para>
        /// </summary>
        /// <param name="gameObject"><see cref="UnityEngine.GameObject"/>。</param>
        private static bool IsHierarchyObject(GameObject gameObject)
        {
            if (gameObject == null || !gameObject.scene.IsValid())
            {
                return false;
            }

            return !EditorSceneManager.IsPreviewScene(gameObject.scene) ||
                   PrefabStageUtility.GetPrefabStage(gameObject) != null;
        }

        /// <summary>
        ///   <para>索引所有者绑定。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="flag">标记。</param>
        private static void IndexOwnerBindings(Component owner, BindingFlag flag)
        {
            using var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var variables = serializedObject.FindProperty(UIComponentEditorUtility.VariablesField);
            if (variables == null || !variables.isArray)
            {
                return;
            }

            for (var i = 0; i < variables.arraySize; i++)
            {
                var variable = variables.GetArrayElementAtIndex(i);
                var value = variable.FindPropertyRelative(UIComponentEditorUtility.VariableValueField)?.objectReferenceValue;
                var referencedComponent = value as Component;
                var node = UICompositionPolicy.GetReferenceNode(value);
                if (node == null)
                {
                    continue;
                }

                var id = node.GetInstanceID();
                if (!s_Bindings.TryGetValue(id, out var binding))
                {
                    binding.NameWidth = EditorStyles.label.CalcSize(new GUIContent(node.name)).x;
                }

                var eventIds = variable.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField);
                var variableName = variable.FindPropertyRelative(UIComponentEditorUtility.VariableNameField)?.stringValue;
                binding.flag |= flag;
                AddBindingReference(
                    ref binding,
                    variableName,
                    referencedComponent,
                    GetBindingOwnerLabel(serializedObject, flag));
                AddBoundEvents(ref binding, variableName, referencedComponent, eventIds);
                s_Bindings[id] = binding;
            }
        }

        /// <summary>
        ///   <para>获取绑定所有者标签。</para>
        /// </summary>
        /// <param name="serializedObject">序列化对象。</param>
        /// <param name="flag">标记。</param>
        private static string GetBindingOwnerLabel(
            SerializedObject serializedObject,
            BindingFlag flag)
        {
            var fieldName = flag == BindingFlag.View
                ? UIComponentEditorUtility.ViewTypeNameField
                : UIComponentEditorUtility.WidgetTypeNameField;
            var type = UIComponentEditorUtility.ReadType(serializedObject.FindProperty(fieldName));
            var label = type?.FullName;
            if (string.IsNullOrEmpty(label))
            {
                label = flag == BindingFlag.View ? "未指定 View" : "未指定 Widget";
            }

            return flag == BindingFlag.View
                ? $"View：{label}"
                : $"Widget：{label}";
        }

        /// <summary>
        ///   <para>添加绑定引用。</para>
        /// </summary>
        /// <param name="binding">绑定。</param>
        /// <param name="variableName">变量名称。</param>
        /// <param name="component">组件。</param>
        /// <param name="ownerLabel">所有者标签。</param>
        private static void AddBindingReference(
            ref NodeBinding binding,
            string variableName,
            Component component,
            string ownerLabel)
        {
            if (component == null || string.IsNullOrEmpty(ownerLabel))
            {
                return;
            }

            var name = string.IsNullOrEmpty(variableName)
                ? component.GetType().Name
                : variableName;
            var typeName = component.GetType().Name;
            binding.BindingReferences ??= new List<string>(2);
            binding.BindingReferences.Add($"{name}: {typeName} -> {ownerLabel}");
        }

        /// <summary>
        ///   <para>添加已绑定事件。</para>
        /// </summary>
        /// <param name="binding">绑定。</param>
        /// <param name="variableName">变量名称。</param>
        /// <param name="component">组件。</param>
        /// <param name="eventIds">事件标识列表。</param>
        private static void AddBoundEvents(
            ref NodeBinding binding,
            string variableName,
            Component component,
            SerializedProperty eventIds)
        {
            if (component == null || eventIds == null || !eventIds.isArray || eventIds.arraySize == 0)
            {
                return;
            }

            var definitions = UIEventsGenerator.GetEventDefinitions(component.GetType());
            var variableLabel = string.IsNullOrEmpty(variableName)
                ? component.GetType().Name
                : variableName;
            for (var definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                var definition = definitions[definitionIndex];
                if (!ContainsEventId(eventIds, definition.Name))
                {
                    continue;
                }

                binding.EventCount++;
                if (binding.EventNames != null && binding.EventNames.Count >= MaxTooltipEventCount)
                {
                    continue;
                }

                binding.EventNames ??= new List<string>(MaxTooltipEventCount);
                binding.EventNames.Add($"{variableLabel}.{definition.DisplayName}");
            }
        }

        /// <summary>
        ///   <para>包含事件标识。</para>
        /// </summary>
        /// <param name="eventIds">事件标识列表。</param>
        /// <param name="eventId">事件标识。</param>
        private static bool ContainsEventId(SerializedProperty eventIds, string eventId)
        {
            for (var i = 0; i < eventIds.arraySize; i++)
            {
                if (string.Equals(
                        eventIds.GetArrayElementAtIndex(i).stringValue,
                        eventId,
                        System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>标记绑定缓存失效。</para>
        /// </summary>
        private static void InvalidateBindings()
        {
            s_Dirty = true;
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        ///   <para>处理序列化属性修改。</para>
        /// </summary>
        /// <param name="modifications">修改。</param>
        private static UndoPropertyModification[] OnPostprocessModifications(
            UndoPropertyModification[] modifications)
        {
            for (var i = 0; i < modifications.Length; i++)
            {
                var value = modifications[i].currentValue;
                if (value == null || value.target == null)
                {
                    continue;
                }

                var isUIComponent = value.target is UIViewComponent || value.target is UIWidgetComponent;
                var path = value.propertyPath;
                if (isUIComponent &&
                    (path.StartsWith(
                         UIComponentEditorUtility.VariablesField,
                         System.StringComparison.Ordinal) ||
                     string.Equals(
                         path,
                         UIComponentEditorUtility.ViewTypeNameField,
                         System.StringComparison.Ordinal) ||
                     string.Equals(
                         path,
                         UIComponentEditorUtility.WidgetTypeNameField,
                         System.StringComparison.Ordinal)))
                {
                    InvalidateBindings();
                    break;
                }
            }

            return modifications;
        }

        /// <summary>
        ///   <para>创建绑定内容。</para>
        /// </summary>
        /// <param name="binding">绑定。</param>
        private static GUIContent CreateBindingContent(NodeBinding binding)
        {
            if (binding.BindingReferences == null || binding.BindingReferences.Count == 0)
            {
                return new GUIContent(string.Empty, "UI 绑定");
            }

            return new GUIContent(
                string.Empty,
                string.Join("\n", binding.BindingReferences));
        }

        /// <summary>
        ///   <para>创建事件内容。</para>
        /// </summary>
        /// <param name="binding">绑定。</param>
        private static GUIContent CreateEventContent(NodeBinding binding)
        {
            var tooltip = $"事件：{binding.EventCount}";
            for (var i = 0; i < binding.EventNames.Count; i++)
            {
                tooltip += $"\n{binding.EventNames[i]}";
            }

            if (binding.EventCount > binding.EventNames.Count)
            {
                tooltip += "\n...";
            }

            var label = binding.EventCount > MaxVisibleEventCount
                ? "..."
                : binding.EventCount.ToString();
            return new GUIContent(label, tooltip);
        }

        /// <summary>
        ///   <para>事件数量样式。</para>
        /// </summary>
        private static GUIStyle EventCountStyle => s_EventCountStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            fontSize = 8,
            fontStyle = FontStyle.Bold,
            margin = new RectOffset(),
            padding = new RectOffset(),
            contentOffset = Vector2.zero,
            normal = { textColor = Color.white }
        };

        /// <summary>
        ///   <para>获取标记颜色。</para>
        /// </summary>
        /// <param name="flag">标记。</param>
        private static Color GetBadgeColor(BindingFlag flag)
        {
            var hasView = (flag & BindingFlag.View) != 0;
            var hasWidget = (flag & BindingFlag.Widget) != 0;
            return hasView && hasWidget
                ? new Color(1f, 0.75f, 0.35f)
                : hasView
                    ? new Color(0.35f, 0.75f, 1f)
                    : new Color(0.45f, 1f, 0.55f);
        }

        /// <summary>
        ///   <para>节点绑定。</para>
        /// </summary>
        private struct NodeBinding
        {
            /// <summary>
            ///   <para>标记。</para>
            /// </summary>
            internal BindingFlag flag;
            /// <summary>
            ///   <para>事件数量。</para>
            /// </summary>
            internal int EventCount;
            /// <summary>
            ///   <para>名称宽度。</para>
            /// </summary>
            internal float NameWidth;
            /// <summary>
            ///   <para>绑定引用。</para>
            /// </summary>
            internal List<string> BindingReferences;
            /// <summary>
            ///   <para>事件名称。</para>
            /// </summary>
            internal List<string> EventNames;
            /// <summary>
            ///   <para>绑定内容。</para>
            /// </summary>
            internal GUIContent BindingContent;
            /// <summary>
            ///   <para>事件内容。</para>
            /// </summary>
            internal GUIContent EventContent;
        }

        /// <summary>
        ///   <para>绑定目标。</para>
        /// </summary>
        private readonly struct BindingTarget
        {
            /// <summary>
            ///   <para>所有者。</para>
            /// </summary>
            internal readonly Component Owner;
            /// <summary>
            ///   <para>值。</para>
            /// </summary>
            internal readonly Component Value;

            /// <summary>
            ///   <para>创建绑定目标。</para>
            /// </summary>
            /// <param name="owner">所有者。</param>
            /// <param name="value">值。</param>
            internal BindingTarget(Component owner, Component value)
            {
                Owner = owner;
                Value = value;
            }
        }

        /// <summary>
        ///   <para>绑定键。</para>
        /// </summary>
        private readonly struct BindingKey : IEquatable<BindingKey>
        {
            /// <summary>
            ///   <para>所有者索引。</para>
            /// </summary>
            private readonly int ownerIndex;
            /// <summary>
            ///   <para>所有者标签。</para>
            /// </summary>
            private readonly string ownerLabel;
            /// <summary>
            ///   <para>组件类型。</para>
            /// </summary>
            private readonly Type componentType;
            /// <summary>
            ///   <para>组件索引。</para>
            /// </summary>
            private readonly int componentIndex;

            /// <summary>
            ///   <para>创建绑定键。</para>
            /// </summary>
            /// <param name="ownerIndex">所有者索引。</param>
            /// <param name="ownerLabel">所有者标签。</param>
            /// <param name="componentType">组件类型。</param>
            /// <param name="componentIndex">组件索引。</param>
            internal BindingKey(int ownerIndex, string ownerLabel, Type componentType, int componentIndex)
            {
                this.ownerIndex = ownerIndex;
                this.ownerLabel = ownerLabel;
                this.componentType = componentType;
                this.componentIndex = componentIndex;
            }

            /// <inheritdoc />
            public bool Equals(BindingKey other)
            {
                return ownerIndex == other.ownerIndex &&
                       string.Equals(ownerLabel, other.ownerLabel, StringComparison.Ordinal) &&
                       componentType == other.componentType && componentIndex == other.componentIndex;
            }

            /// <inheritdoc />
            public override bool Equals(object obj) => obj is BindingKey other && Equals(other);
            /// <inheritdoc />
            public override int GetHashCode() => ownerIndex ^
                                                  (ownerLabel?.GetHashCode() ?? 0) ^
                                                  (componentType?.GetHashCode() ?? 0) ^ componentIndex;
        }

        /// <summary>
        ///   <para>绑定候选。</para>
        /// </summary>
        private readonly struct BindingCandidate
        {
            /// <summary>
            ///   <para>键。</para>
            /// </summary>
            internal readonly BindingKey Key;
            /// <summary>
            ///   <para>所有者标签。</para>
            /// </summary>
            internal readonly string OwnerLabel;
            /// <summary>
            ///   <para>组件标签。</para>
            /// </summary>
            internal readonly string ComponentLabel;
            /// <summary>
            ///   <para>目标。</para>
            /// </summary>
            internal readonly BindingTarget Target;

            /// <summary>
            ///   <para>创建绑定候选。</para>
            /// </summary>
            /// <param name="key">键。</param>
            /// <param name="ownerLabel">所有者标签。</param>
            /// <param name="componentLabel">组件标签。</param>
            /// <param name="target">目标。</param>
            internal BindingCandidate(BindingKey key, string ownerLabel, string componentLabel, BindingTarget target)
            {
                Key = key;
                OwnerLabel = ownerLabel;
                ComponentLabel = componentLabel;
                Target = target;
            }
        }

        /// <summary>
        ///   <para>绑定选项。</para>
        /// </summary>
        private readonly struct BindingOption
        {
            /// <summary>
            ///   <para>所有者。</para>
            /// </summary>
            internal readonly Component Owner;
            /// <summary>
            ///   <para>所有者标签。</para>
            /// </summary>
            internal readonly string OwnerLabel;
            /// <summary>
            ///   <para>组件标签。</para>
            /// </summary>
            internal readonly string ComponentLabel;
            /// <summary>
            ///   <para>目标。</para>
            /// </summary>
            internal readonly IReadOnlyList<BindingTarget> Targets;

            /// <summary>
            ///   <para>创建绑定选项。</para>
            /// </summary>
            /// <param name="owner">所有者。</param>
            /// <param name="ownerLabel">所有者标签。</param>
            /// <param name="componentLabel">组件标签。</param>
            /// <param name="targets">目标。</param>
            internal BindingOption(
                Component owner,
                string ownerLabel,
                string componentLabel,
                IReadOnlyList<BindingTarget> targets)
            {
                Owner = owner;
                OwnerLabel = ownerLabel;
                ComponentLabel = componentLabel;
                Targets = targets;
            }
        }

        /// <summary>
        ///   <para>绑定标记。</para>
        /// </summary>
        [System.Flags]
        private enum BindingFlag : byte
        {
            /// <summary>
            ///   <para>无。</para>
            /// </summary>
            None = 0,
            /// <summary>
            ///   <para>页面。</para>
            /// </summary>
            View = 1,
            /// <summary>
            ///   <para>部件。</para>
            /// </summary>
            Widget = 2,
        }
    }
}

#endif
