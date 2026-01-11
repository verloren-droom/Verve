#if UNITY_EDITOR

namespace Verve.Editor
{
    using Verve;
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using UnityEditorInternal;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>能力表单资产自定义编辑器</para>
    /// </summary>
    [CustomEditor(typeof(CapabilitySheetAsset))]
    internal sealed class CapabilitySheetAssetEditor : Editor
    {
        private CapabilitySheetAsset m_Target;
        private ReorderableList m_CapabilityList;
        private ReorderableList m_ComponentList;
        private ReorderableList m_SubSheetList;

        private string m_CapabilitySearchQuery = "";
        private string m_ComponentSearchQuery = "";
        private Vector2 m_CapabilityScrollPos;
        private Vector2 m_ComponentScrollPos;

        private bool m_ShowCapabilities = true;
        private bool m_ShowComponents = true;
        private bool m_ShowSubSheets = true;

        private List<Type> m_CachedCapabilityTypes;
        private List<Type> m_CachedComponentTypes;
        private Vector2 m_TypePickerScrollPos;
        private Vector2 m_CodeScrollPos;

        private static class Styles
        {
            public static readonly GUIStyle BoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };
        }

        private void OnEnable()
        {
            m_Target = target as CapabilitySheetAsset;
            
            InitializeCapabilityList();
            InitializeComponentList();
            InitializeSubSheetList();
            
            CacheAvailableTypes();
        }

        public override void OnInspectorGUI()
        {
            if (m_Target == null) return;
            
            serializedObject.Update();

            DrawDescription();
            
            EditorGUILayout.Space(10);
            DrawCapabilitiesSection();
            
            EditorGUILayout.Space(10);
            DrawComponentsSection();
            
            EditorGUILayout.Space(10);
            DrawSubSheetsSection();
            
            EditorGUILayout.Space(10);
            DrawShowSheetCode();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorUtility.SetDirty(m_Target);
        }

        private void DrawDescription()
        {
            EditorGUILayout.BeginVertical(Styles.BoxStyle);
            
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            m_Target.Description = EditorGUILayout.TextArea(
                m_Target.Description, 
                GUILayout.Height(60)
            );
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCapabilitiesSection()
        {
            EditorGUILayout.BeginVertical(Styles.BoxStyle);
            
            m_ShowCapabilities = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_ShowCapabilities,
                $"Capabilities ({m_Target.CapabilityTypeEntries.Count})"
            );

            if (m_ShowCapabilities)
            {
                EditorGUILayout.Space(5);
                
                m_CapabilitySearchQuery = EditorGUILayout.TextField(m_CapabilitySearchQuery, EditorStyles.toolbarSearchField);

                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("+ Add Capability Type", GUILayout.Height(25)))
                {
                    ShowCapabilityTypePicker();
                }

                EditorGUILayout.Space(5);
                m_CapabilityList.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawComponentsSection()
        {
            EditorGUILayout.BeginVertical(Styles.BoxStyle);
            
            m_ShowComponents = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_ShowComponents,
                $"Components ({m_Target.ComponentTypeEntries.Count})"
            );

            if (m_ShowComponents)
            {
                EditorGUILayout.Space(5);
                
                m_ComponentSearchQuery = EditorGUILayout.TextField(m_ComponentSearchQuery, EditorStyles.toolbarSearchField);

                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("+ Add Component Type", GUILayout.Height(25)))
                {
                    ShowComponentTypePicker();
                }

                EditorGUILayout.Space(5);
                m_ComponentList.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawSubSheetsSection()
        {
            EditorGUILayout.BeginVertical(Styles.BoxStyle);
            
            m_ShowSubSheets = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_ShowSubSheets,
                $"Sub-Sheets ({m_Target.SubSheets.Count})"
            );

            if (m_ShowSubSheets)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "Sub-sheets are recursively applied when this sheet is used.", 
                    MessageType.Info
                );
                EditorGUILayout.Space(5);
                
                m_SubSheetList.DoLayoutList();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawShowSheetCode()
        {
            EditorGUILayout.BeginVertical(Styles.BoxStyle);
            
            EditorGUILayout.LabelField("Generated Sheet Code", EditorStyles.boldLabel);

            string variableName = "sheet";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"var {variableName} = new {nameof(CapabilitySheet)}();");
            sb.AppendLine();
            
            if (m_Target.CapabilityTypeEntries.Count > 0)
            {
                sb.AppendLine("// Capabilities");
                foreach (var entry in m_Target.CapabilityTypeEntries)
                {
                    if (entry.IsValid)
                    {
                        sb.AppendLine($"{variableName}.{nameof(CapabilitySheet.AddCapability)}<{entry.GetSystemType().Name}>();");
                    }
                }
                sb.AppendLine();
            }
            
            if (m_Target.ComponentTypeEntries.Count > 0)
            {
                sb.AppendLine("// Components");
                foreach (var entry in m_Target.ComponentTypeEntries)
                {
                    if (!entry.IsValid)
                        continue;

                    var type = entry.GetSystemType();
                    if (type == null)
                        continue;

                    var direction = entry.Direction;
                    if (direction == NetworkSyncDirection.None)
                    {
                        sb.AppendLine($"{variableName}.{nameof(CapabilitySheet.AddComponent)}<{type.Name}>();");
                    }
                    else
                    {
                        var directionCode = $"{nameof(NetworkSyncDirection)}.{direction}";
                        sb.AppendLine($"{variableName}.{nameof(CapabilitySheet.AddComponent)}<{type.Name}>({directionCode});");
                    }
                }
                sb.AppendLine();
            }

            if (m_Target.SubSheets?.Count > 0)
            {
                sb.AppendLine("// Sub-sheets");
                foreach (var subSheet in m_Target.SubSheets)
                {
                    if (subSheet != null)
                    {
                        sb.AppendLine($"{variableName}.{nameof(CapabilitySheet.AddSubSheet)}(/* Reference to: {subSheet.name} */);");
                    }
                }
                sb.AppendLine();
            }
            
            string codeText = sb.ToString().TrimEnd('\n');
    
            m_CodeScrollPos = EditorGUILayout.BeginScrollView(
                m_CodeScrollPos,
                GUILayout.MinHeight(Mathf.Min(150, Mathf.Max(200, codeText.Split('\n').Length * 16f))),
                GUILayout.MaxHeight(250),
                GUILayout.MaxWidth(600)
            );
    
            GUILayout.TextArea(codeText, EditorStyles.textArea, GUILayout.ExpandHeight(true));
    
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                EditorGUIUtility.systemCopyBuffer = codeText;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void InitializeCapabilityList()
        {
            m_CapabilityList = new ReorderableList(
                m_Target.CapabilityTypeEntries,
                typeof(CapabilitySheetAsset.TypeEntry),
                true, true, false, true
            );

            m_CapabilityList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Capability Types");
            };

            m_CapabilityList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= m_Target.CapabilityTypeEntries.Count)
                    return;

                var entry = m_Target.CapabilityTypeEntries[index];
                
                if (!string.IsNullOrEmpty(m_CapabilitySearchQuery) &&
                    !entry.TypeName.ToLower().Contains(m_CapabilitySearchQuery.ToLower()))
                    return;

                rect.y += 2;
                float iconWidth = 20;
                float nameWidth = rect.width - iconWidth - 10;

                Rect iconRect = new Rect(rect.x, rect.y, iconWidth, EditorGUIUtility.singleLineHeight);
                if (entry.IsValid)
                {
                    EditorGUI.LabelField(iconRect, new GUIContent("✓", "Valid"));
                }
                else
                {
                    EditorGUI.LabelField(iconRect, new GUIContent("✗", "Invalid"));
                }

                Rect nameRect = new Rect(
                    rect.x + iconWidth + 5, 
                    rect.y, 
                    nameWidth, 
                    EditorGUIUtility.singleLineHeight
                );
                
                var style = entry.IsValid ? EditorStyles.label : EditorStyles.boldLabel;
                var color = entry.IsValid ? Color.white : Color.red;
                
                var oldColor = GUI.color;
                GUI.color = color;
                EditorGUI.LabelField(nameRect, entry.TypeName, style);
                GUI.color = oldColor;

                if (nameRect.Contains(Event.current.mousePosition))
                {
                    GUI.tooltip = entry.AssemblyQualifiedName;
                }
            };

            m_CapabilityList.onRemoveCallback = list =>
            {
                if (EditorUtility.DisplayDialog(
                    "Remove Capability",
                    $"Remove '{m_Target.CapabilityTypeEntries[list.index].TypeName}'?",
                    "Remove", "Cancel"))
                {
                    m_Target.RemoveCapabilityType(list.index);
                }
            };
        }

        private void InitializeComponentList()
        {
            m_ComponentList = new ReorderableList(
                m_Target.ComponentTypeEntries,
                typeof(CapabilitySheetAsset.TypeEntry),
                true, true, false, true
            );

            m_ComponentList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Component Types");
            };

            m_ComponentList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= m_Target.ComponentTypeEntries.Count)
                    return;

                var entry = m_Target.ComponentTypeEntries[index];
                
                if (!string.IsNullOrEmpty(m_ComponentSearchQuery) &&
                    !entry.TypeName.ToLower().Contains(m_ComponentSearchQuery.ToLower()))
                    return;

                rect.y += 2;
                float iconWidth = 20;
                float directionWidth = 140;
                float nameWidth = rect.width - iconWidth - directionWidth - 15;

                Rect iconRect = new Rect(rect.x, rect.y, iconWidth, EditorGUIUtility.singleLineHeight);
                if (entry.IsValid)
                {
                    EditorGUI.LabelField(iconRect, new GUIContent("✓", "Valid"));
                }
                else
                {
                    EditorGUI.LabelField(iconRect, new GUIContent("✗", "Invalid"));
                }

                Rect nameRect = new Rect(
                    rect.x + iconWidth + 5, 
                    rect.y, 
                    nameWidth, 
                    EditorGUIUtility.singleLineHeight
                );
                
                Rect directionRect = new Rect(
                    rect.x + iconWidth + 10 + nameWidth,
                    rect.y,
                    directionWidth,
                    EditorGUIUtility.singleLineHeight
                );
                
                var style = entry.IsValid ? EditorStyles.label : EditorStyles.boldLabel;
                var color = entry.IsValid ? Color.white : Color.red;
                
                var oldColor = GUI.color;
                GUI.color = color;
                EditorGUI.LabelField(nameRect, entry.TypeName, style);
                GUI.color = oldColor;

                if (nameRect.Contains(Event.current.mousePosition))
                {
                    GUI.tooltip = entry.AssemblyQualifiedName;
                }

                var newDirection = (NetworkSyncDirection)EditorGUI.EnumPopup(directionRect, entry.Direction);
                if (newDirection != entry.Direction)
                {
                    entry.Direction = newDirection;
                    EditorUtility.SetDirty(m_Target);
                }
            };

            m_ComponentList.onRemoveCallback = list =>
            {
                if (EditorUtility.DisplayDialog(
                    "Remove Component",
                    $"Remove '{m_Target.ComponentTypeEntries[list.index].TypeName}'?",
                    "Remove", "Cancel"))
                {
                    m_Target.RemoveComponentType(list.index);
                }
            };
        }

        private void InitializeSubSheetList()
        {
            m_SubSheetList = new ReorderableList(
                m_Target.SubSheets,
                typeof(CapabilitySheetAsset),
                true, true, true, true
            );

            m_SubSheetList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Sub-Sheet Assets");
            };

            m_SubSheetList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= m_Target.SubSheets.Count)
                    return;

                rect.y += 2;
                
                var oldSheet = m_Target.SubSheets[index];
                var newSheet = EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    oldSheet,
                    typeof(CapabilitySheetAsset),
                    false
                ) as CapabilitySheetAsset;

                if (newSheet != oldSheet)
                {
                    if (newSheet == null)
                    {
                        m_Target.SubSheets[index] = null;
                    }
                    else if (newSheet == m_Target)
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Reference",
                            "Cannot reference self as sub-sheet!",
                            "OK"
                        );
                    }
                    else
                    {
                        m_Target.SubSheets[index] = newSheet;
                    }
                }
            };

            m_SubSheetList.onAddCallback = list =>
            {
                m_Target.SubSheets.Add(null);
            };

            m_SubSheetList.onRemoveCallback = list =>
            {
                m_Target.RemoveSubSheet(list.index);
            };
        }

        private void CacheAvailableTypes()
        {
            m_CachedCapabilityTypes = TypeCache.GetTypesDerivedFrom<Capability>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .ToList();
        
            m_CachedComponentTypes = TypeCache.GetTypesDerivedFrom<IComponent>()
                .Where(t => t.IsValueType && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();
        }
        
        private void ShowCapabilityTypePicker()
        {
            var menu = new GenericMenu();

            foreach (var type in m_CachedCapabilityTypes)
            {
                var typeName = type.Name;
                var fullName = type.FullName;
                
                bool alreadyAdded = m_Target.CapabilityTypeEntries
                    .Any(e => e.AssemblyQualifiedName == type.AssemblyQualifiedName);

                if (alreadyAdded)
                {
                    menu.AddDisabledItem(new GUIContent(fullName));
                }
                else
                {
                    menu.AddItem(
                        new GUIContent(fullName),
                        false,
                        () => {
                            m_Target.AddCapabilityType(type);
                            EditorUtility.SetDirty(m_Target);
                        }
                    );
                }
            }

            if (m_CachedCapabilityTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Capability types found"));
            }

            menu.ShowAsContext();
        }

        private void ShowComponentTypePicker()
        {
            var menu = new GenericMenu();

            foreach (var type in m_CachedComponentTypes)
            {
                var fullName = type.FullName;
                
                bool alreadyAdded = m_Target.ComponentTypeEntries
                    .Any(e => e.AssemblyQualifiedName == type.AssemblyQualifiedName);

                if (alreadyAdded)
                {
                    menu.AddDisabledItem(new GUIContent(fullName));
                }
                else
                {
                    menu.AddItem(
                        new GUIContent(fullName),
                        false,
                        () => {
                            m_Target.AddComponentType(type);
                            EditorUtility.SetDirty(m_Target);
                        }
                    );
                }
            }

            if (m_CachedComponentTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Component types found"));
            }

            menu.ShowAsContext();
        }
    }
}

#endif
