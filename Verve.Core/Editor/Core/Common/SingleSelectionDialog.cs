#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    
    /// <summary>
    ///   <para>简单单选下拉框弹窗</para>
    ///   <para>注意：此窗口不会阻塞线程，使用回调函数处理结果</para>
    /// </summary>
    public class SingleSelectionDialog : EditorWindow
    {
        private string m_Description;
        private string m_SelectedText;
        private string m_CancelText;
        private string[] m_Options;
        private int m_SelectedIndex;
        private Action<int> m_OnSelected;
        private Action m_OnCancel;
        
        private static readonly GUIStyle s_LabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 13,
            padding = new RectOffset(12, 12, 8, 4),
            margin = new RectOffset(8, 8, 0, 0),
            wordWrap = true,
            richText = true
        };
        
        private static readonly GUIStyle s_PopupStyle = new GUIStyle(EditorStyles.popup)
        {
            fixedHeight = 24,
            fontSize = 12,
            margin = new RectOffset(10, 10, 0, 0),
            padding = new RectOffset(4, 4, 2, 2)
        };
        
        private static readonly GUIStyle s_ButtonStyle = new GUIStyle("Button")
        {
            fixedHeight = 24,
            fixedWidth = 80,
            fontSize = 12,
            margin = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(6, 6, 2, 2)
        };
        
        /// <summary>
        ///   <para>显示单选弹窗</para>
        /// </summary>
        public static void Show(string title, string description, string[] options, Action<int> onSelectedCallback, string selectedText = "Ok", Action onCancelCallback = null, string cancelText = "Cancel")
        {
            if (options == null || options.Length == 0)
            {
                return;
            }
            string result = null;
            var window = CreateInstance<SingleSelectionDialog>();
            window.titleContent = new GUIContent(title);
            window.m_Description = description;
            window.m_Options = options;
            window.m_SelectedIndex = 0;
            window.m_SelectedText = selectedText;
            window.m_OnSelected = (index) => 
            {
                onSelectedCallback?.Invoke(index);
                result = options[index];
            };
            window.m_CancelText = cancelText;
            window.m_OnCancel = () => 
            {
                onCancelCallback?.Invoke();
                result = null;
            };
        
            window.minSize = new Vector2(300, 150);
            window.maxSize = new Vector2(400, 200);
        
            window.ShowUtility();
        }
        
        void OnGUI()
        {
            if (EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            }
            else
            {
                GUI.backgroundColor = new Color(0.76f, 0.76f, 0.76f);
            }
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(m_Description))
            {
                EditorGUILayout.LabelField(m_Description, s_LabelStyle);
                EditorGUILayout.Space(12);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_SelectedIndex = EditorGUILayout.Popup(m_SelectedIndex, m_Options, s_PopupStyle, 
                GUILayout.Width(250), GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (!string.IsNullOrEmpty(m_CancelText) && GUILayout.Button(m_CancelText, s_ButtonStyle))
            {
                m_OnCancel?.Invoke();
                Close();
            }
            EditorGUILayout.Space(15);
            GUI.backgroundColor = new Color(0.25f, 0.5f, 0.9f);
            if (!string.IsNullOrEmpty(m_SelectedText) && GUILayout.Button(m_SelectedText, s_ButtonStyle))
            {
                m_OnSelected?.Invoke(m_SelectedIndex);
                Close();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        m_OnSelected?.Invoke(m_SelectedIndex);
                        Close();
                        Event.current.Use();
                        break;
                    case KeyCode.Escape:
                        m_OnCancel?.Invoke();
                        Close();
                        Event.current.Use();
                        break;
                }
            }
        }
    }
}

#endif