// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    ///   <para>时间轴与 Actor Inspector 共用的只读实时组件视图。</para>
    /// </summary>
    internal sealed class ACCComponentView : VisualElement
    {
        private const double SampleInterval = 0.2d;
        private const int MaxFields = 128;
        private readonly Dictionary<Type, object> m_Values = new();
        private readonly Dictionary<Type, ComponentRow> m_Rows = new();
        private readonly HashSet<Type> m_Observed = new();
        private readonly List<Type> m_Types = new();
        private readonly Label m_Context = new();
        private readonly Label m_Empty = new();
        private readonly ToolbarSearchField m_Search = new();
        private readonly ToolbarMenu m_Options = new() { tooltip = "组件显示选项" };
        private readonly ScrollView m_Scroll = new(ScrollViewMode.Vertical);
        private World m_World;
        private Actor m_Actor = Actor.none;
        private bool m_Sampled;
        private bool m_Alive;
        private bool m_OnlyChanged;
        private double m_NextSample;

        internal ACCComponentView(Toolbar header = null)
        {
            name = "acc-components";
            style.flexGrow = 1f;
            style.minHeight = 0f;
            var toolbar = header ?? new Toolbar();
            m_Context.style.flexGrow = 1f;
            m_Context.style.minWidth = 0f;
            m_Context.style.overflow = Overflow.Hidden;
            m_Context.style.textOverflow = TextOverflow.Ellipsis;
            m_Context.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_Context.style.fontSize = 11f;
            toolbar.Add(m_Context);
            m_Search.name = "acc-component-search";
            m_Search.tooltip = "按组件完整类型名搜索";
            m_Search.style.width = 160f;
            m_Search.style.minWidth = 100f;
            m_Search.style.flexShrink = 1f;
            m_Search.RegisterValueChangedCallback(_ => ApplyFilter());
            toolbar.Add(m_Search);
            var options = m_Options;
            options.Q(className: ToolbarMenu.textUssClassName).style.display = DisplayStyle.None;
            options.Q(className: ToolbarMenu.arrowUssClassName).style.display = DisplayStyle.None;
            options.Add(new Image { image = EditorGUIUtility.IconContent("_Popup").image,
                pickingMode = PickingMode.Ignore, style = { width = 16f, height = 16f } });
            options.menu.AppendAction("展开所有组件", _ => SetAllExpanded(true),
                _ => HasCollapsedComponent() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            options.menu.AppendAction("折叠所有组件", _ => SetAllExpanded(false),
                _ => HasExpandedComponent() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            options.menu.AppendAction("仅显示变化项", _ =>
            {
                m_OnlyChanged = !m_OnlyChanged;
                ApplyFilter();
            }, _ => m_OnlyChanged ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            options.menu.AppendAction("重置变化标记", _ =>
            {
                foreach (var row in m_Rows.Values)
                {
                    row.changed = false;
                    row.added = false;
                    foreach (var field in row.fields.Values)
                    {
                        field.changed = false;
                        field.highlightUntil = 0d;
                        field.element.style.backgroundColor = Color.clear;
                    }
                }
                ApplyFilter();
            });
            toolbar.Add(options);
            if (header == null) Add(toolbar);
            m_Empty.style.paddingLeft = 8f;
            m_Empty.style.paddingTop = 6f;
            Add(m_Empty);
            m_Scroll.style.flexGrow = 1f;
            m_Scroll.style.minHeight = 0f;
            m_Scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Add(m_Scroll);
        }

        internal void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            m_Context.style.display = m_Search.style.display = m_Options.style.display = style.display;
        }

        /// <summary>
        ///   <para>由宿主在视图可见时采样；帧号始终取运行时，不使用时间轴播放头。</para>
        /// </summary>
        internal void Refresh(World world, Actor actor)
        {
            if (!ReferenceEquals(world, m_World) || actor != m_Actor)
            {
                ClearRows();
                m_World = world;
                m_Actor = actor;
            }
            var now = EditorApplication.timeSinceStartup;
            if (world != null && !world.IsDisposed && !actor.IsNone)
            {
                if (style.display == DisplayStyle.None || now < m_NextSample) return;
                m_NextSample = now + SampleInterval;
            }
            m_Observed.Clear();
            foreach (var pair in m_Rows)
            {
                var row = pair.Value;
                // 只观察展开且在视口中的组件；变化筛选时继续观察已展开项。
                if (row.foldout.value && MatchesSearch(pair.Key) && (panel == null || m_OnlyChanged ||
                    row.foldout.worldBound.Overlaps(m_Scroll.contentViewport.worldBound)))
                    m_Observed.Add(pair.Key);
            }
            if (!ACCComponentReader.Read(world, actor, m_Values, m_Observed))
            {
                ClearRows();
                m_Context.text = "实时组件";
                m_Empty.text = world == null || world.IsDisposed ? "无运行中的 Actor" : "Actor 已销毁";
                m_Empty.style.display = DisplayStyle.Flex;
                return;
            }
            m_Alive = true;
            m_Context.text = $"{actor} · 实时组件 · 帧 {Time.frameCount}";
            m_Context.tooltip = "当前运行时数据，最多每秒采样 5 次；仅观察展开的组件，拖动时间尺不会回放组件值。";
            foreach (var pair in m_Rows)
                if (!m_Values.ContainsKey(pair.Key) && pair.Value.present)
                {
                    pair.Value.present = false;
                    pair.Value.changed = true;
                    pair.Value.foldout.Clear();
                    pair.Value.fields.Clear();
                    pair.Value.sampled = false;
                }
            m_Types.Clear();
            m_Types.AddRange(m_Values.Keys);
            m_Types.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            foreach (var type in m_Types)
            {
                if (!m_Rows.TryGetValue(type, out var row))
                {
                    row = new ComponentRow(type);
                    row.changed = row.added = m_Sampled;
                    row.foldout.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.target != row.foldout) return;
                        m_NextSample = 0d;
                    });
                    m_Rows.Add(type, row);
                    m_Scroll.Add(row.foldout);
                }
                if (!row.present)
                {
                    row.changed = row.added = true;
                }
                row.present = true;
                var value = m_Values[type];
                if (value == null) continue;
                row.fieldCount = 0;
                foreach (var field in row.fields.Values) field.seen = false;
                foreach (var field in ACCComponentReader.GetFields(type))
                {
                    if (row.fieldCount >= MaxFields) break;
                    ReadField(row, field.Name, field.FieldType, field.GetValue(value), 0, now);
                }
                row.stale.Clear();
                foreach (var pair in row.fields)
                    if (!pair.Value.seen) row.stale.Add(pair.Key);
                foreach (var key in row.stale)
                {
                    row.fields[key].element.RemoveFromHierarchy();
                    row.fields.Remove(key);
                    row.changed = true;
                }
                if (row.fields.Count == 0 && row.foldout.childCount == 0)
                    row.foldout.Add(new Label("标记组件（无可显示字段）"));
                row.foldout.tooltip = row.fieldCount >= MaxFields
                    ? type.FullName + "\n最多显示 128 个字段；数组最多 32 项，嵌套最多 4 层。" : type.FullName;
                row.sampled = true;
            }
            m_Sampled = true;
            ApplyFilter();
        }

        private bool HasCollapsedComponent()
        {
            foreach (var row in m_Rows.Values)
                if (!row.foldout.value && row.present) return true;
            return false;
        }

        private bool HasExpandedComponent()
        {
            foreach (var row in m_Rows.Values)
                if (row.foldout.value && row.present) return true;
            return false;
        }

        private void SetAllExpanded(bool expanded)
        {
            foreach (var row in m_Rows.Values)
                if (row.present && row.foldout.value != expanded)
                    row.foldout.value = expanded;
            m_NextSample = 0d;
            ApplyFilter();
        }

        private void ClearRows()
        {
            m_Values.Clear();
            m_Rows.Clear();
            m_Scroll.Clear();
            m_Observed.Clear();
            m_Types.Clear();
            m_Sampled = false;
            m_Alive = false;
            m_NextSample = 0d;
        }

        private void ReadField(ComponentRow row, string path, Type type, object value, int depth, double now)
        {
            if (row.fieldCount >= MaxFields) return;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (value != null && depth < 4 && !IsSimple(type))
            {
                if (value is Array array && array.Rank == 1)
                {
                    ReadField(row, path + ".Length", typeof(int), array.Length, 4, now);
                    var limit = Math.Min(array.Length, 32);
                    for (var i = 0; i < limit; i++)
                        ReadField(row, path + "[" + i + "]", type.GetElementType(), array.GetValue(i + array.GetLowerBound(0)), depth + 1, now);
                    if (array.Length > limit) ReadField(row, path + "[…]", typeof(string), "仅显示前 32 项", 4, now);
                    return;
                }
                if (type.IsValueType)
                {
                    var fields = ACCComponentReader.GetFields(type);
                    if (fields.Length > 0)
                    {
                        foreach (var field in fields)
                            ReadField(row, path + "." + field.Name, field.FieldType, field.GetValue(value), depth + 1, now);
                        return;
                    }
                }
            }
            if (value == null || !IsSimple(type))
            {
                value = value == null ? "null" : type.Name + "（未展开）";
                type = typeof(string);
            }
            row.fieldCount++;
            if (!row.fields.TryGetValue(path, out var state) || state.type != type)
            {
                state?.element.RemoveFromHierarchy();
                state = CreateField(path, type, value);
                state.changed = row.sampled || row.added;
                state.highlightUntil = state.changed ? now + 0.6d : 0d;
                row.changed |= state.changed;
                row.fields[path] = state;
                row.foldout.Add(state.element);
            }
            else if (!Equals(state.value, value))
            {
                state.changed = row.changed = true;
                state.highlightUntil = now + 0.6d;
                state.update(value);
            }
            state.value = value;
            state.seen = true;
            state.element.style.backgroundColor = now < state.highlightUntil
                ? new Color(0.95f, 0.7f, 0.2f, 0.18f) : Color.clear;
        }

        private void ApplyFilter()
        {
            if (!m_Alive) return;
            var visible = 0;
            foreach (var pair in m_Rows)
            {
                var row = pair.Value;
                var status = !row.present ? "已移除" : row.added ? "已添加" : row.changed ? "有变化" : "存在";
                if (row.status != status)
                {
                    row.status = status;
                    row.foldout.text = row.label + " · " + status;
                }
                var matches = MatchesSearch(pair.Key);
                var show = matches && (!m_OnlyChanged || row.changed);
                row.foldout.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show) visible++;
                foreach (var field in row.fields.Values)
                    field.element.style.display = !m_OnlyChanged || field.changed
                        ? DisplayStyle.Flex : DisplayStyle.None;
            }
            m_Empty.text = m_Rows.Count == 0 ? "Actor 没有组件" : "没有匹配的组件";
            m_Empty.style.display = visible == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private bool MatchesSearch(Type type)
            => (type.FullName ?? type.Name).IndexOf(m_Search.value ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsSimple(Type type)
            => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
               type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(Color) ||
               type == typeof(Rect) || type == typeof(Bounds) || typeof(UnityEngine.Object).IsAssignableFrom(type);

        private static FieldRow CreateField(string label, Type type, object value)
        {
            if (type == typeof(bool)) return Field(new Toggle(label), (bool)value, type);
            if (type == typeof(int)) return Field(new IntegerField(label) { isReadOnly = true }, (int)value, type);
            if (type == typeof(long)) return Field(new LongField(label) { isReadOnly = true }, (long)value, type);
            if (type == typeof(float)) return Field(new FloatField(label) { isReadOnly = true }, (float)value, type);
            if (type == typeof(double)) return Field(new DoubleField(label) { isReadOnly = true }, (double)value, type);
            if (type == typeof(Vector2)) return Field(new Vector2Field(label), (Vector2)value, type);
            if (type == typeof(Vector3)) return Field(new Vector3Field(label), (Vector3)value, type);
            if (type == typeof(Vector4)) return Field(new Vector4Field(label), (Vector4)value, type);
            if (type == typeof(Vector2Int)) return Field(new Vector2IntField(label), (Vector2Int)value, type);
            if (type == typeof(Vector3Int)) return Field(new Vector3IntField(label), (Vector3Int)value, type);
            if (type == typeof(Color)) return Field(new ColorField(label), (Color)value, type);
            if (type == typeof(Rect)) return Field(new RectField(label), (Rect)value, type);
            if (type == typeof(Bounds)) return Field(new BoundsField(label), (Bounds)value, type);
            if (type.IsEnum) return Field(new EnumField(label, (Enum)value), (Enum)value, type);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return Field(new ObjectField(label) { objectType = type }, (UnityEngine.Object)value, type);
            var text = new TextField(label) { isReadOnly = true };
            var result = Field(text, Convert.ToString(value, CultureInfo.InvariantCulture), type);
            result.update = current => text.SetValueWithoutNotify(Convert.ToString(current, CultureInfo.InvariantCulture));
            return result;
        }

        private static FieldRow Field<T>(BaseField<T> field, T value, Type type)
        {
            field.name = "acc-field-" + field.label;
            field.SetValueWithoutNotify(value);
            field.labelElement.style.minWidth = 120f;
            field.labelElement.style.width = new Length(40f, LengthUnit.Percent);
            field.labelElement.style.overflow = Overflow.Hidden;
            field.labelElement.style.textOverflow = TextOverflow.Ellipsis;
            field.tooltip = field.label;
            if (!(field is TextField || field is IntegerField || field is LongField || field is FloatField || field is DoubleField))
                field.SetEnabled(false);
            return new FieldRow { type = type, element = field, update = current => field.SetValueWithoutNotify((T)current) };
        }

        private sealed class ComponentRow
        {
            internal readonly Foldout foldout;
            internal readonly Dictionary<string, FieldRow> fields = new();
            internal readonly List<string> stale = new();
            internal readonly string label;
            internal string status = "存在";
            internal bool present = true;
            internal bool changed;
            internal bool added;
            internal bool sampled;
            internal int fieldCount;
            internal ComponentRow(Type type)
            {
                label = type.Name;
                foldout = new Foldout { text = label + " · 存在", value = true, tooltip = type.FullName };
            }
        }

        private sealed class FieldRow
        {
            internal Type type;
            internal VisualElement element;
            internal Action<object> update;
            internal object value;
            internal bool seen;
            internal bool changed;
            internal double highlightUntil;
        }
    }
}

#endif
