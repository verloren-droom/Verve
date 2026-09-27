#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///   <para><see cref="ActorObject"/> Inspector；配置表单和对象级禁用能力。</para>
    /// </summary>
    [CustomEditor(typeof(ActorObject))]
    sealed class ActorObjectEditor : UnityEditor.Editor
    {
        /// <summary>
        ///   <para>当前行动者对象。</para>
        /// </summary>
        private ActorObject m_ActorObject;
        /// <summary>
        ///   <para>世界名称属性。</para>
        /// </summary>
        private SerializedProperty m_WorldName;
        /// <summary>
        ///   <para>初始表单属性。</para>
        /// </summary>
        private SerializedProperty m_InitialSheet;
        /// <summary>
        ///   <para>禁用能力类型名称列表。</para>
        /// </summary>
        private SerializedProperty m_DisabledCapabilityTypeNames;
        /// <summary>
        ///   <para>能力区域展开状态。</para>
        /// </summary>
        private bool m_ShowCapabilities = true;
        /// <summary>
        ///   <para>组件区域展开状态。</para>
        /// </summary>
        private bool m_ShowComponents;
        /// <summary>
        ///   <para>运行时状态区域展开状态。</para>
        /// </summary>
        private bool m_ShowRuntimeState = true;

        /// <summary>
        ///   <para>初始化序列化属性。</para>
        /// </summary>
        private void OnEnable()
        {
            m_ActorObject = (ActorObject)target;
            m_WorldName = serializedObject.FindProperty("m_WorldName");
            m_InitialSheet = serializedObject.FindProperty("m_InitialSheet");
            m_DisabledCapabilityTypeNames = serializedObject.FindProperty("m_DisabledCapabilityTypeNames");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            if (m_ActorObject == null)
                return;

            serializedObject.Update();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(m_WorldName, new GUIContent("世界"));
                EditorGUILayout.PropertyField(m_InitialSheet, new GUIContent("初始表单"));
                EditorGUILayout.Space(4f);
                DrawCapabilities();
                EditorGUILayout.Space(4f);
                DrawComponents();
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            DrawRuntimeState();
        }

        /// <summary>
        ///   <para>绘制表单能力及其启用状态。</para>
        /// </summary>
        private void DrawCapabilities()
        {
            var sheet = GetInitialSheet();
            var sourceTypes = CollectTypes(sheet, true);
            if (!CoreEditorUtility.DrawHeaderToggle(
                    new GUIContent($"能力 ({sourceTypes.Count})"),
                    ref m_ShowCapabilities))
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (sourceTypes.Count == 0)
                {
                    EditorGUILayout.LabelField(
                        sheet == null ? "未选择初始表单。" : "表单未配置能力。",
                        EditorStyles.miniLabel);
                    DrawInvalidDisabledCapabilities(sourceTypes);
                    return;
                }

                for (var i = 0; i < sourceTypes.Count; i++)
                    DrawCapability(sourceTypes[i]);

                DrawInvalidDisabledCapabilities(sourceTypes);
            }
        }

        /// <summary>
        ///   <para>绘制表单组件；组件由表单统一管理。</para>
        /// </summary>
        private void DrawComponents()
        {
            var componentTypes = CollectTypes(GetInitialSheet(), false);
            if (!CoreEditorUtility.DrawHeaderToggle(
                    new GUIContent($"组件 ({componentTypes.Count})"),
                    ref m_ShowComponents))
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (componentTypes.Count == 0)
                {
                    EditorGUILayout.LabelField("表单未配置组件。", EditorStyles.miniLabel);
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    for (var i = 0; i < componentTypes.Count; i++)
                        EditorGUILayout.LabelField(GetTypeLabel(componentTypes[i]));
                }
            }
        }

        /// <summary>
        ///   <para>绘制能力启用开关。</para>
        /// </summary>
        /// <param name="type">表单能力类型。</param>
        private void DrawCapability(Type type)
        {
            var enabled = !IsDisabled(type);
            var wasEnabled = enabled;
            var expanded = false;
            CoreEditorUtility.DrawHeaderToggle(
                new GUIContent(GetTypeLabel(type), type.FullName),
                ref expanded,
                ref enabled);
            if (enabled != wasEnabled)
                SetCapabilityEnabled(type, enabled);
        }

        /// <summary>
        ///   <para>绘制运行时信息；运行时始终为只读状态。</para>
        /// </summary>
        private void DrawRuntimeState()
        {
            if (!Application.isPlaying || !CoreEditorUtility.DrawHeaderToggle(
                    new GUIContent("运行时状态"),
                    ref m_ShowRuntimeState))
                return;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("已创建", m_ActorObject.IsCreated);
                EditorGUILayout.TextField("行动者", m_ActorObject.Actor.ToString());
                EditorGUILayout.TextField(
                    "世界",
                    m_ActorObject.World == null ? "无" : m_ActorObject.World.Name);
            }
        }

        /// <summary>
        ///   <para>设置当前对象的能力启用状态。</para>
        /// </summary>
        /// <param name="type">表单能力类型。</param>
        /// <param name="enabled">是否启用。</param>
        private void SetCapabilityEnabled(Type type, bool enabled)
        {
            Undo.RecordObject(m_ActorObject, "Edit Actor Capabilities");
            var typeName = type.AssemblyQualifiedName;
            for (var i = m_DisabledCapabilityTypeNames.arraySize - 1; i >= 0; i--)
            {
                var item = m_DisabledCapabilityTypeNames.GetArrayElementAtIndex(i);
                if (item.stringValue == typeName)
                    m_DisabledCapabilityTypeNames.DeleteArrayElementAtIndex(i);
            }

            if (!enabled)
            {
                var index = m_DisabledCapabilityTypeNames.arraySize;
                m_DisabledCapabilityTypeNames.InsertArrayElementAtIndex(index);
                m_DisabledCapabilityTypeNames.GetArrayElementAtIndex(index).stringValue = typeName;
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            EditorUtility.SetDirty(m_ActorObject);
            Repaint();
        }

        /// <summary>
        ///   <para>判断能力是否已被当前对象禁用。</para>
        /// </summary>
        /// <param name="type">能力类型。</param>
        /// <returns>是否禁用。</returns>
        private bool IsDisabled(Type type)
        {
            var typeName = type.AssemblyQualifiedName;
            for (var i = 0; i < m_DisabledCapabilityTypeNames.arraySize; i++)
            {
                if (m_DisabledCapabilityTypeNames.GetArrayElementAtIndex(i).stringValue == typeName)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///   <para>显示无法解析或不在表单中的禁用项。</para>
        /// </summary>
        /// <param name="sourceTypes">表单能力类型。</param>
        private void DrawInvalidDisabledCapabilities(IReadOnlyList<Type> sourceTypes)
        {
            var sourceNames = new HashSet<string>(
                sourceTypes.Select(type => type.AssemblyQualifiedName),
                StringComparer.Ordinal);
            for (var i = 0; i < m_DisabledCapabilityTypeNames.arraySize; i++)
            {
                var typeName = m_DisabledCapabilityTypeNames.GetArrayElementAtIndex(i).stringValue;
                var type = ResolveType(typeName);
                if (type == null || !sourceNames.Contains(typeName))
                {
                    EditorGUILayout.HelpBox(
                        $"禁用能力不存在：{type?.FullName ?? typeName}",
                        MessageType.Warning);
                    return;
                }
            }
        }

        /// <summary>
        ///   <para>递归收集表单类型并去重。</para>
        /// </summary>
        /// <param name="sheet">表单资产。</param>
        /// <param name="capabilities">是否收集能力。</param>
        /// <returns>类型列表。</returns>
        private static List<Type> CollectTypes(CapabilitySheetAsset sheet, bool capabilities)
        {
            var result = new List<Type>();
            CollectTypes(
                sheet,
                capabilities,
                result,
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<CapabilitySheetAsset>());
            return result;
        }

        /// <summary>
        ///   <para>递归收集当前表单及子表单类型。</para>
        /// </summary>
        /// <param name="sheet">当前表单。</param>
        /// <param name="capabilities">是否收集能力。</param>
        /// <param name="result">结果列表。</param>
        /// <param name="typeNames">已收集类型名称。</param>
        /// <param name="visited">已访问表单。</param>
        private static void CollectTypes(
            CapabilitySheetAsset sheet,
            bool capabilities,
            List<Type> result,
            HashSet<string> typeNames,
            HashSet<CapabilitySheetAsset> visited)
        {
            if (sheet == null || !visited.Add(sheet))
                return;

            var entries = capabilities ? sheet.CapabilityTypeEntries : sheet.ComponentTypeEntries;
            for (var i = 0; i < entries.Count; i++)
            {
                var type = entries[i].GetSystemType();
                if (type != null && typeNames.Add(type.AssemblyQualifiedName))
                    result.Add(type);
            }

            for (var i = 0; i < sheet.SubSheets.Count; i++)
                CollectTypes(sheet.SubSheets[i], capabilities, result, typeNames, visited);
        }

        /// <summary>
        ///   <para>获取当前初始表单。</para>
        /// </summary>
        /// <returns>初始表单；未选择时为空。</returns>
        private CapabilitySheetAsset GetInitialSheet()
            => m_InitialSheet.objectReferenceValue as CapabilitySheetAsset;

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
    }
}

#endif
