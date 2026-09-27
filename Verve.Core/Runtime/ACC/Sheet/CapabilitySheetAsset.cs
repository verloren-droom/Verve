#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para><see cref="CapabilitySheet"/>能力表单资产。</para>
    /// </summary>
    [CreateAssetMenu(fileName = "New Capability Sheet", menuName = "Verve/Capability Sheet")]
    public sealed class CapabilitySheetAsset : ScriptableObject
    {
        /// <summary>
        ///   <para>类型条目；保存可序列化的程序集限定类型名。</para>
        /// </summary>
        [Serializable]
        public class TypeEntry
        {
            /// <summary>
            ///   <para>编辑器中显示的类型名称。</para>
            /// </summary>
            [SerializeField] private string m_TypeName;
            /// <summary>
            ///   <para>用于运行时解析的程序集限定类型名。</para>
            /// </summary>
            [SerializeField] private string m_AssemblyQualifiedName;
            /// <summary>
            ///   <para>TypeName；编辑器显示名称。</para>
            /// </summary>
            public string TypeName
            {
                get => m_TypeName;
                set => m_TypeName = value;
            }
            /// <summary>
            ///   <para>AssemblyQualifiedName；运行时类型解析名称。</para>
            /// </summary>
            public string AssemblyQualifiedName
            {
                get => m_AssemblyQualifiedName;
                set => m_AssemblyQualifiedName = value;
            }
            /// <summary>
            ///   <para>IsValid；类型名称当前是否可以解析。</para>
            /// </summary>
            public bool IsValid => GetSystemType() != null;
            /// <summary>
            ///   <para>创建空类型条目。</para>
            /// </summary>
            public TypeEntry() { }
            /// <summary>
            ///   <para>从类型创建能力条目。</para>
            /// </summary>
            /// <param name="type">能力或组件类型。</param>
            public TypeEntry(Type type)
            {
                if (type == null)
                {
                    return;
                }

                m_TypeName = type.Name;
                m_AssemblyQualifiedName = type.AssemblyQualifiedName;
            }
            /// <summary>
            ///   <para>解析程序集限定类型。</para>
            /// </summary>
            /// <returns>解析到的类型；名称为空或解析失败时返回 <see langword="null"/>。</returns>
            public Type GetSystemType()
            {
                if (string.IsNullOrEmpty(m_AssemblyQualifiedName)) return null;
                try
                {
                    return Type.GetType(m_AssemblyQualifiedName, false);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
        }
        
        /// <summary>
        ///   <para>表单编辑器描述。</para>
        /// </summary>
        [SerializeField, TextArea(2, 4), Tooltip("表单描述")] private string m_Description = "Capability Sheet Description";
        /// <summary>
        ///   <para>序列化的能力类型条目。</para>
        /// </summary>
        [SerializeField, Tooltip("能力类型")] private List<TypeEntry> m_CapabilityTypes = new List<TypeEntry>();
        /// <summary>
        ///   <para>序列化的组件类型条目。</para>
        /// </summary>
        [SerializeField, Tooltip("组件类型")] private List<TypeEntry> m_ComponentTypes = new List<TypeEntry>();
        /// <summary>
        ///   <para>序列化的子表单引用。</para>
        /// </summary>
        [SerializeField, Tooltip("子表单")] private List<CapabilitySheetAsset> m_SubSheets = new List<CapabilitySheetAsset>();

        /// <summary>
        ///   <para>表单描述。</para>
        /// </summary>
        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        /// <summary>
        ///   <para>能力类型条目列表。</para>
        /// </summary>
        public IReadOnlyList<TypeEntry> CapabilityTypeEntries => m_CapabilityTypes;

        /// <summary>
        ///   <para>组件类型条目列表。</para>
        /// </summary>
        public IReadOnlyList<TypeEntry> ComponentTypeEntries => m_ComponentTypes;

        /// <summary>
        ///   <para>子表单列表。</para>
        /// </summary>
        public IReadOnlyList<CapabilitySheetAsset> SubSheets => m_SubSheets;

        /// <summary>
        ///   <para>清理编辑器中的自身引用；空引用用于保留待填写的子表单条目。</para>
        /// </summary>
        private void OnValidate()
        {
            m_SubSheets.RemoveAll(s => s == this);
        }

        /// <summary>
        ///   <para>添加能力类型。</para>
        /// </summary>
        public bool AddCapabilityType(Type capabilityType)
        {
            if (capabilityType == null || !typeof(Capability).IsAssignableFrom(capabilityType))
                return false;

            for (var i = 0; i < m_CapabilityTypes.Count; i++)
                if (m_CapabilityTypes[i]?.AssemblyQualifiedName == capabilityType.AssemblyQualifiedName)
                    return false;

            m_CapabilityTypes.Add(new TypeEntry(capabilityType));
            return true;
        }

        /// <summary>
        ///   <para>移除能力类型。</para>
        /// </summary>
        public bool RemoveCapabilityType(int index)
        {
            if (index < 0 || index >= m_CapabilityTypes.Count)
                return false;

            m_CapabilityTypes.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>添加组件类型。</para>
        /// </summary>
        public bool AddComponentType(Type componentType)
        {
            if (componentType == null ||
                !typeof(IComponent).IsAssignableFrom(componentType) ||
                !componentType.IsValueType)
                return false;

            for (var i = 0; i < m_ComponentTypes.Count; i++)
                if (m_ComponentTypes[i]?.AssemblyQualifiedName == componentType.AssemblyQualifiedName)
                    return false;

            m_ComponentTypes.Add(new TypeEntry(componentType));
            return true;
        }

        /// <summary>
        ///   <para>移除组件类型。</para>
        /// </summary>
        public bool RemoveComponentType(int index)
        {
            if (index < 0 || index >= m_ComponentTypes.Count)
                return false;

            m_ComponentTypes.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>添加子表单。</para>
        /// </summary>
        public bool AddSubSheet(CapabilitySheetAsset subSheet)
        {
            if (subSheet == null || subSheet == this)
                return false;

            if (m_SubSheets.Contains(subSheet))
                return false;

            if (HasCircularReference(subSheet))
                return false;

            m_SubSheets.Add(subSheet);
            return true;
        }

        /// <summary>
        ///   <para>移除子表单。</para>
        /// </summary>
        public bool RemoveSubSheet(int index)
        {
            if (index < 0 || index >= m_SubSheets.Count)
                return false;

            m_SubSheets.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>设置子表单引用。</para>
        /// </summary>
        public bool SetSubSheet(int index, CapabilitySheetAsset subSheet)
        {
            if (index < 0 || index >= m_SubSheets.Count)
                return false;

            if (subSheet == this || (subSheet != null && HasCircularReference(subSheet)))
                return false;

            m_SubSheets[index] = subSheet;
            return true;
        }

        /// <summary>
        ///   <para>转换为运行时 <see cref="CapabilitySheet"/> 对象。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CapabilitySheet ToSheet()
        {
            return ToSheet(new HashSet<CapabilitySheetAsset>());
        }

        /// <summary>
        ///   <para>递归转换表单资产并检测当前路径中的循环引用。</para>
        /// </summary>
        /// <param name="path">当前递归路径。</param>
        private CapabilitySheet ToSheet(HashSet<CapabilitySheetAsset> path)
        {
            if (!path.Add(this))
                throw new InvalidOperationException($"能力表单存在循环引用：{name}。");

            var sheet = new CapabilitySheet();
            try
            {
                for (var i = 0; i < m_CapabilityTypes.Count; i++)
                {
                    var entry = m_CapabilityTypes[i];
                    var type = entry?.GetSystemType();
                    if (type == null)
                        throw new InvalidOperationException(
                            $"无法解析能力类型：{entry?.TypeName ?? "<空条目>"} ({entry?.AssemblyQualifiedName ?? "<空>"})。");
                    sheet.AddCapability(type);
                }

                for (var i = 0; i < m_ComponentTypes.Count; i++)
                {
                    var entry = m_ComponentTypes[i];
                    var type = entry?.GetSystemType();
                    if (type == null)
                        throw new InvalidOperationException(
                            $"无法解析组件类型：{entry?.TypeName ?? "<空条目>"} ({entry?.AssemblyQualifiedName ?? "<空>"})。");
                    sheet.AddComponent(type);
                }

                for (var i = 0; i < m_SubSheets.Count; i++)
                {
                    var subSheetAsset = m_SubSheets[i];
                    if (subSheetAsset != null)
                        sheet.AddSubSheet(subSheetAsset.ToSheet(path));
                }

                return sheet;
            }
            finally
            {
                path.Remove(this);
            }
        }

        /// <summary>
        ///   <para>从运行时 <see cref="CapabilitySheet"/> 对象导入。</para>
        /// </summary>
        /// <param name="sheet">待导入的运行时表单。</param>
        public void FromSheet(CapabilitySheet sheet)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            m_CapabilityTypes.Clear();
            m_ComponentTypes.Clear();

            foreach (var type in sheet.CapabilityTypes)
                AddCapabilityType(type);

            foreach (var type in sheet.ComponentTypes)
                AddComponentType(type);
        }
        /// <summary>
        ///   <para>检查添加目标是否会形成循环引用。</para>
        /// </summary>
        /// <param name="target">待检查子表单。</param>
        private bool HasCircularReference(CapabilitySheetAsset target)
        {
            var visited = new HashSet<CapabilitySheetAsset>();
            return HasCircularReferenceRecursive(target, visited);
        }
        /// <summary>
        ///   <para>递归检查子表单引用图。</para>
        /// </summary>
        /// <param name="target">当前检查节点。</param>
        /// <param name="visited">已访问节点集合。</param>
        private bool HasCircularReferenceRecursive(CapabilitySheetAsset target, HashSet<CapabilitySheetAsset> visited)
        {
            if (target == null)
                return false;

            if (target == this)
                return true;

            if (!visited.Add(target))
                return false;

            foreach (var subSheet in target.m_SubSheets)
            {
                if (HasCircularReferenceRecursive(subSheet, visited))
                    return true;
            }

            return false;
        }
    }
}

#endif