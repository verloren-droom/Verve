#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    /// <summary>
    ///   <para><see cref="CapabilitySheetAsset"/> Inspector；编辑能力、组件和子表单。</para>
    /// </summary>
    [CustomEditor(typeof(CapabilitySheetAsset))]
    sealed class CapabilitySheetAssetEditor : UnityEditor.Editor
    {
        /// <summary>
        ///   <para>当前表单资产。</para>
        /// </summary>
        private CapabilitySheetAsset m_Sheet;
        /// <summary>
        ///   <para>描述属性。</para>
        /// </summary>
        private SerializedProperty m_Description;
        /// <summary>
        ///   <para>能力条目属性。</para>
        /// </summary>
        private SerializedProperty m_CapabilityTypes;
        /// <summary>
        ///   <para>组件条目属性。</para>
        /// </summary>
        private SerializedProperty m_ComponentTypes;
        /// <summary>
        ///   <para>子表单属性。</para>
        /// </summary>
        private SerializedProperty m_SubSheets;
        /// <summary>
        ///   <para>能力列表。</para>
        /// </summary>
        private ReorderableList m_CapabilityList;
        /// <summary>
        ///   <para>组件列表。</para>
        /// </summary>
        private ReorderableList m_ComponentList;
        /// <summary>
        ///   <para>子表单列表。</para>
        /// </summary>
        private ReorderableList m_SubSheetList;
        /// <summary>
        ///   <para>可添加的能力类型。</para>
        /// </summary>
        private List<Type> m_CapabilityOptions;
        /// <summary>
        ///   <para>可添加的组件类型。</para>
        /// </summary>
        private List<Type> m_ComponentOptions;
        /// <summary>
        ///   <para>代码预览区域展开状态。</para>
        /// </summary>
        private bool m_ShowCode;
        /// <summary>
        ///   <para>代码预览滚动位置。</para>
        /// </summary>
        private Vector2 m_CodeScrollPosition;

        /// <summary>
        ///   <para>初始化序列化属性和列表。</para>
        /// </summary>
        private void OnEnable()
        {
            m_Sheet = (CapabilitySheetAsset)target;
            m_Description = serializedObject.FindProperty("m_Description");
            m_CapabilityTypes = serializedObject.FindProperty("m_CapabilityTypes");
            m_ComponentTypes = serializedObject.FindProperty("m_ComponentTypes");
            m_SubSheets = serializedObject.FindProperty("m_SubSheets");

            CacheTypes();
            m_CapabilityList = CreateTypeList(
                m_CapabilityTypes,
                "能力",
                m_Sheet.CapabilityTypeEntries,
                RemoveCapability,
                AddCapability);
            m_ComponentList = CreateTypeList(
                m_ComponentTypes,
                "组件",
                m_Sheet.ComponentTypeEntries,
                RemoveComponent,
                AddComponent);
            m_SubSheetList = CreateSubSheetList();
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            if (m_Sheet == null)
                return;

            serializedObject.Update();
            EditorGUILayout.LabelField("描述", EditorStyles.boldLabel);
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var description = EditorGUILayout.TextArea(
                    m_Description.stringValue,
                    EditorStyles.textArea,
                    GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3f));
                if (change.changed)
                    m_Description.stringValue = description;
            }
            EditorGUILayout.Space(4f);

            m_CapabilityList.DoLayoutList();
            EditorGUILayout.Space(4f);
            m_ComponentList.DoLayoutList();
            EditorGUILayout.Space(4f);
            m_SubSheetList.DoLayoutList();

            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
                EditorUtility.SetDirty(m_Sheet);
        }

        /// <summary>
        ///   <para>初始化类型列表。</para>
        /// </summary>
        /// <param name="property">序列化列表。</param>
        /// <param name="header">列表标题。</param>
        /// <param name="entries">类型条目。</param>
        /// <param name="remove">移除回调。</param>
        /// <param name="add">添加回调。</param>
        private ReorderableList CreateTypeList(
            SerializedProperty property,
            string header,
            IReadOnlyList<CapabilitySheetAsset.TypeEntry> entries,
            Action<int> remove,
            Action<Rect> add)
        {
            var list = new ReorderableList(serializedObject, property, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6f
            };
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"{header} ({entries.Count})");
            list.onAddDropdownCallback = (rect, _) => add(rect);
            list.drawElementCallback = (rect, index, _, _) =>
            {
                if ((uint)index >= (uint)property.arraySize)
                    return;

                var element = property.GetArrayElementAtIndex(index);
                var assemblyName = element.FindPropertyRelative("m_AssemblyQualifiedName").stringValue;
                var type = ResolveType(assemblyName);
                var label = type == null
                    ? element.FindPropertyRelative("m_TypeName").stringValue + "（类型不可用）"
                    : GetTypeLabel(type);
                rect.y += 2f;
                EditorGUI.LabelField(rect, label, type == null ? EditorStyles.boldLabel : EditorStyles.label);
            };
            list.onRemoveCallback = listInstance =>
            {
                if ((uint)listInstance.index >= (uint)entries.Count)
                    return;
                if (!EditorUtility.DisplayDialog(
                        "移除类型",
                        $"确定移除“{entries[listInstance.index].TypeName}”吗？",
                        "移除",
                        "取消"))
                    return;

                Undo.RecordObject(m_Sheet, $"Remove {header}");
                remove(listInstance.index);
                serializedObject.Update();
                Repaint();
            };
            return list;
        }

        /// <summary>
        ///   <para>初始化子表单列表。</para>
        /// </summary>
        private ReorderableList CreateSubSheetList()
        {
            var list = new ReorderableList(serializedObject, m_SubSheets, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6f
            };
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"子表单资源 ({m_Sheet.SubSheets.Count})");
            list.drawElementCallback = (rect, index, _, _) =>
            {
                if ((uint)index >= (uint)m_SubSheets.arraySize)
                    return;
                rect.y += 2f;
                var oldSheet = m_Sheet.SubSheets[index];
                var newSheet = EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    oldSheet,
                    typeof(CapabilitySheetAsset),
                    false) as CapabilitySheetAsset;
                if (newSheet == oldSheet)
                    return;

                Undo.RecordObject(m_Sheet, "Set Sub-Sheet");
                if (!m_Sheet.SetSubSheet(index, newSheet))
                {
                    EditorUtility.DisplayDialog(
                        "无效的子表单",
                        "子表单不能引用自身或形成循环引用。",
                        "确定");
                    return;
                }

                serializedObject.Update();
                EditorUtility.SetDirty(m_Sheet);
            };
            list.onAddCallback = listInstance =>
            {
                Undo.RecordObject(m_Sheet, "Add Sub-Sheet");
                var property = listInstance.serializedProperty;
                var index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                EditorUtility.SetDirty(m_Sheet);
                Repaint();
            };
            list.onRemoveCallback = listInstance =>
            {
                if ((uint)listInstance.index >= (uint)m_Sheet.SubSheets.Count)
                    return;
                Undo.RecordObject(m_Sheet, "Remove Sub-Sheet");
                m_Sheet.RemoveSubSheet(listInstance.index);
                serializedObject.Update();
                EditorUtility.SetDirty(m_Sheet);
            };
            return list;
        }

        /// <summary>
        ///   <para>缓存可用能力和组件类型。</para>
        /// </summary>
        private void CacheTypes()
        {
            m_CapabilityOptions = TypeCache.GetTypesDerivedFrom<Capability>()
                .Where(IsSelectableCapability)
                .OrderBy(GetTypeLabel, StringComparer.Ordinal)
                .ToList();
            m_ComponentOptions = TypeCache.GetTypesDerivedFrom<IComponent>()
                .Where(IsSelectableComponent)
                .OrderBy(GetTypeLabel, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        ///   <para>显示能力类型选择弹窗。</para>
        /// </summary>
        /// <param name="rect">按钮区域。</param>
        private void AddCapability(Rect rect)
        {
            ShowTypePopup(rect, m_CapabilityOptions, m_Sheet.CapabilityTypeEntries, type =>
            {
                Undo.RecordObject(m_Sheet, "Add Capability");
                if (m_Sheet.AddCapabilityType(type))
                {
                    serializedObject.Update();
                    EditorUtility.SetDirty(m_Sheet);
                    Repaint();
                }
            }, "能力");
        }

        /// <summary>
        ///   <para>显示组件类型选择弹窗。</para>
        /// </summary>
        /// <param name="rect">按钮区域。</param>
        private void AddComponent(Rect rect)
        {
            ShowTypePopup(rect, m_ComponentOptions, m_Sheet.ComponentTypeEntries, type =>
            {
                Undo.RecordObject(m_Sheet, "Add Component");
                if (m_Sheet.AddComponentType(type))
                {
                    serializedObject.Update();
                    EditorUtility.SetDirty(m_Sheet);
                    Repaint();
                }
            }, "组件");
        }

        /// <summary>
        ///   <para>显示可搜索类型弹窗。</para>
        /// </summary>
        /// <param name="rect">弹窗锚点。</param>
        /// <param name="types">候选类型。</param>
        /// <param name="entries">已有条目。</param>
        /// <param name="selected">选择回调。</param>
        /// <param name="title">弹窗标题。</param>
        private static void ShowTypePopup(
            Rect rect,
            IReadOnlyList<Type> types,
            IReadOnlyList<CapabilitySheetAsset.TypeEntry> entries,
            Action<Type> selected,
            string title)
        {
            var available = new List<Type>(types.Count);
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if (entries.Any(entry => entry.AssemblyQualifiedName == type.AssemblyQualifiedName))
                    continue;
                available.Add(type);
            }

            if (available.Count == 0)
                return;

            var labels = new string[available.Count];
            var labelToType = new Dictionary<string, Type>(StringComparer.Ordinal);
            for (var i = 0; i < available.Count; i++)
            {
                var label = GetPopupLabel(available[i]);
                while (!labelToType.TryAdd(label, available[i]))
                    label += " ";
                labels[i] = label;
            }

            PopupWindow.Show(
                rect,
                new SearchableOptionsPopup(
                    string.Empty,
                    labels,
                    label =>
                    {
                        if (labelToType.TryGetValue(label, out var type))
                            selected(type);
                    },
                    $"ACC_{title}",
                    $"添加{title}",
                    "/"));
        }

        /// <summary>
        ///   <para>移除能力条目。</para>
        /// </summary>
        /// <param name="index">条目索引。</param>
        private void RemoveCapability(int index) => m_Sheet.RemoveCapabilityType(index);

        /// <summary>
        ///   <para>移除组件条目。</para>
        /// </summary>
        /// <param name="index">条目索引。</param>
        private void RemoveComponent(int index) => m_Sheet.RemoveComponentType(index);

        /// <summary>
        ///   <para>创建代码预览。</para>
        /// </summary>
        /// <returns>代码文本。</returns>
        private string BuildCodePreview()
        {
            var lines = new List<string> { "var sheet = new CapabilitySheet();", string.Empty };
            AppendCodeLines(lines, m_Sheet, new HashSet<CapabilitySheetAsset>());
            return string.Join("\n", lines).TrimEnd();
        }

        /// <summary>
        ///   <para>递归追加代码预览。</para>
        /// </summary>
        /// <param name="lines">代码行。</param>
        /// <param name="sheet">表单资产。</param>
        /// <param name="visited">已访问表单。</param>
        private static void AppendCodeLines(
            List<string> lines,
            CapabilitySheetAsset sheet,
            HashSet<CapabilitySheetAsset> visited)
        {
            if (sheet == null || !visited.Add(sheet))
                return;

            for (var i = 0; i < sheet.CapabilityTypeEntries.Count; i++)
            {
                var type = sheet.CapabilityTypeEntries[i].GetSystemType();
                if (type != null)
                    lines.Add($"sheet.AddCapability<{type.Name}>();");
            }

            for (var i = 0; i < sheet.ComponentTypeEntries.Count; i++)
            {
                var type = sheet.ComponentTypeEntries[i].GetSystemType();
                if (type != null)
                    lines.Add($"sheet.AddComponent<{type.Name}>();");
            }

            for (var i = 0; i < sheet.SubSheets.Count; i++)
            {
                if (sheet.SubSheets[i] == null)
                    continue;
                lines.Add($"// Sub-sheet: {sheet.SubSheets[i].name}");
                AppendCodeLines(lines, sheet.SubSheets[i], visited);
            }
        }

        /// <summary>
        ///   <para>判断能力类型是否可以加入表单。</para>
        /// </summary>
        private static bool IsSelectableCapability(Type type)
            => type.IsDefined(typeof(CapabilitySheetTypeAttribute), false) &&
               !type.IsAbstract && !type.IsGenericType &&
               typeof(Capability).IsAssignableFrom(type) &&
               type.GetConstructor(Type.EmptyTypes) != null;

        /// <summary>
        ///   <para>判断组件类型是否可以加入表单。</para>
        /// </summary>
        private static bool IsSelectableComponent(Type type)
            => type.IsDefined(typeof(CapabilitySheetTypeAttribute), false) &&
               type.IsValueType && !type.IsEnum && !type.IsGenericType &&
               typeof(IComponent).IsAssignableFrom(type);

        /// <summary>
        ///   <para>解析程序集限定类型名。</para>
        /// </summary>
        /// <param name="assemblyQualifiedName">程序集限定类型名。</param>
        /// <returns>解析到的类型。</returns>
        private static Type ResolveType(string assemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
                return null;
            try
            {
                return Type.GetType(assemblyQualifiedName, false);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        ///   <para>获取类型显示名。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>显示名。</returns>
        private static string GetTypeLabel(Type type)
        {
            var metadata = type.GetCustomAttribute<CapabilitySheetTypeAttribute>();
            return string.IsNullOrWhiteSpace(metadata?.DisplayName)
                ? ObjectNames.NicifyVariableName(type.Name)
                : metadata.DisplayName;
        }

        /// <summary>
        ///   <para>获取类型选择项文本；附加完整类型名以便搜索和区分同名类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>选择项文本。</returns>
        private static string GetPopupLabel(Type type)
            => $"{GetTypeLabel(type)} [{type.FullName}]";
    }
}

#endif
