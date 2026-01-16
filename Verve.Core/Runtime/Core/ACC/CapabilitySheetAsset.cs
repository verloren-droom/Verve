#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para><see cref="CapabilitySheet"/>能力表单资产</para>
    /// </summary>
    [CreateAssetMenu(fileName = "New Capability Sheet", menuName = "Verve/Capability Sheet")]
    public sealed class CapabilitySheetAsset : ScriptableObject
    {
        [Serializable]
        public class TypeEntry
        {
            [SerializeField] private string m_TypeName;
            [SerializeField] private string m_AssemblyQualifiedName;
            [SerializeField] private NetworkSyncDirection m_Direction = NetworkSyncDirection.None;

            public string TypeName 
            { 
                get => m_TypeName;
                set => m_TypeName = value;
            }

            public string AssemblyQualifiedName 
            { 
                get => m_AssemblyQualifiedName;
                set => m_AssemblyQualifiedName = value;
            }

            public NetworkSyncDirection Direction
            {
                get => m_Direction;
                set => m_Direction = value;
            }

            public bool IsValid => Type.GetType(m_AssemblyQualifiedName) != null;

            public TypeEntry() { }

            public TypeEntry(Type type)
            {
                if (type == null)
                {
                    return;
                }

                m_TypeName = type.Name;
                m_AssemblyQualifiedName = type.AssemblyQualifiedName;
                m_Direction = NetworkSyncDirection.None;
            }

            public TypeEntry(Type type, NetworkSyncDirection direction)
            {
                if (type == null)
                {
                    return;
                }

                m_TypeName = type.Name;
                m_AssemblyQualifiedName = type.AssemblyQualifiedName;
                m_Direction = direction;
            }

            public Type GetSystemType()
            {
                if (string.IsNullOrEmpty(m_AssemblyQualifiedName))
                {
                    return null;
                }

                try
                {
                    return Type.GetType(m_AssemblyQualifiedName);
                }
                catch
                {
                    return null;
                }
            }
        }

        [SerializeField, TextArea(2, 4), Tooltip("表单描述")] private string m_Description = "Capability Sheet Description";
        [SerializeField, Tooltip("能力类型")] private List<TypeEntry> m_CapabilityTypes = new List<TypeEntry>();
        [SerializeField, Tooltip("组件类型")] private List<TypeEntry> m_ComponentTypes = new List<TypeEntry>();
        [SerializeField, Tooltip("子表单")] private List<CapabilitySheetAsset> m_SubSheets = new List<CapabilitySheetAsset>();

        /// <summary>
        ///   <para>表单描述</para>
        /// </summary>
        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        /// <summary>
        ///   <para>能力类型条目列表</para>
        /// </summary>
        public List<TypeEntry> CapabilityTypeEntries => m_CapabilityTypes;

        /// <summary>
        ///   <para>组件类型条目列表</para>
        /// </summary>
        public List<TypeEntry> ComponentTypeEntries => m_ComponentTypes;

        /// <summary>
        ///   <para>子表单列表</para>
        /// </summary>
        public List<CapabilitySheetAsset> SubSheets => m_SubSheets;

        /// <summary>
        ///   <para>添加能力类型</para>
        /// </summary>
        public bool AddCapabilityType(Type capabilityType)
        {
            if (capabilityType == null || !typeof(Capability).IsAssignableFrom(capabilityType))
                return false;

            if (m_CapabilityTypes.Any(e => e.AssemblyQualifiedName == capabilityType.AssemblyQualifiedName))
                return false;

            m_CapabilityTypes.Add(new TypeEntry(capabilityType));
            return true;
        }

        /// <summary>
        ///   <para>移除能力类型</para>
        /// </summary>
        public bool RemoveCapabilityType(int index)
        {
            if (index < 0 || index >= m_CapabilityTypes.Count)
                return false;

            m_CapabilityTypes.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>添加组件类型</para>
        /// </summary>
        public bool AddComponentType(Type componentType, NetworkSyncDirection direction = NetworkSyncDirection.None)
        {
            if (componentType == null || 
                !typeof(IComponent).IsAssignableFrom(componentType) ||
                !componentType.IsValueType)
                return false;

            if (m_ComponentTypes.Any(e => e.AssemblyQualifiedName == componentType.AssemblyQualifiedName))
                return false;

            m_ComponentTypes.Add(new TypeEntry(componentType, direction));
            return true;
        }

        /// <summary>
        ///   <para>移除组件类型</para>
        /// </summary>
        public bool RemoveComponentType(int index)
        {
            if (index < 0 || index >= m_ComponentTypes.Count)
                return false;

            m_ComponentTypes.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>添加子表单</para>
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
        ///   <para>移除子表单</para>
        /// </summary>
        public bool RemoveSubSheet(int index)
        {
            if (index < 0 || index >= m_SubSheets.Count)
                return false;

            m_SubSheets.RemoveAt(index);
            return true;
        }

        /// <summary>
        ///   <para>转换为运行时Sheet对象</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CapabilitySheet ToSheet()
        {
            var sheet = new CapabilitySheet();

            foreach (var entry in m_CapabilityTypes)
            {
                if (!entry.IsValid) continue;
                
                var type = entry.GetSystemType();
                if (type != null)
                    sheet.AddCapability(type);
            }

            foreach (var entry in m_ComponentTypes)
            {
                if (!entry.IsValid) continue;
                
                var type = entry.GetSystemType();
                if (type != null)
                    sheet.AddComponent(type, entry.Direction);
            }

            foreach (var subSheetAsset in m_SubSheets)
            {
                if (subSheetAsset != null)
                    sheet.AddSubSheet(subSheetAsset.ToSheet());
            }

            sheet.Prewarm();
            return sheet;
        }

        /// <summary>
        ///   <para>从运行时Sheet对象导入</para>
        /// </summary>
        public void FromSheet(CapabilitySheet sheet)
        {
            if (sheet == null)
                return;

            m_CapabilityTypes.Clear();
            m_ComponentTypes.Clear();

            foreach (var type in sheet.CapabilityTypes)
                AddCapabilityType(type);

            var componentTypes = sheet.ComponentTypes;
            var componentDirections = sheet.ComponentDirections;

            for (int i = 0; i < componentTypes.Count; i++)
            {
                var type = componentTypes[i];
                var direction = i < componentDirections.Count ? componentDirections[i] : NetworkSyncDirection.None;
                AddComponentType(type, direction);
            }
        }

        private bool HasCircularReference(CapabilitySheetAsset target)
        {
            var visited = new HashSet<CapabilitySheetAsset>();
            return HasCircularReferenceRecursive(target, visited);
        }

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

        private void OnValidate()
        {
            m_SubSheets.RemoveAll(s => s == null || s == this);
        }
    }
}

#endif