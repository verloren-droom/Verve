#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    
    /// <summary>
    ///   <para>单选弹窗；通过回调返回结果，不阻塞线程。</para>
    /// </summary>
    sealed class SingleSelectionDialog : EditorWindow
    {
        /// <summary>
        ///   <para>描述。</para>
        /// </summary>
        private string m_Description;
        /// <summary>
        ///   <para>选中文本。</para>
        /// </summary>
        private string m_SelectedText;
        /// <summary>
        ///   <para>取消文本。</para>
        /// </summary>
        private string m_CancelText;
        /// <summary>
        ///   <para>选项。</para>
        /// </summary>
        private string[] m_Options;
        /// <summary>
        ///   <para>选中索引。</para>
        /// </summary>
        private int m_SelectedIndex;
        /// <summary>
        ///   <para>选中回调。</para>
        /// </summary>
        private Action<int> m_OnSelected;
        /// <summary>
        ///   <para>取消回调。</para>
        /// </summary>
        private Action m_OnCancel;
        
        /// <summary>
        ///   <para>标签样式。</para>
        /// </summary>
        private static readonly GUIStyle s_LabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 13,
            padding = new RectOffset(12, 12, 8, 4),
            margin = new RectOffset(8, 8, 0, 0),
            wordWrap = true,
            richText = true
        };
        
        /// <summary>
        ///   <para>弹窗样式。</para>
        /// </summary>
        private static readonly GUIStyle s_PopupStyle = new GUIStyle(EditorStyles.popup)
        {
            fixedHeight = 24,
            fontSize = 12,
            margin = new RectOffset(10, 10, 0, 0),
            padding = new RectOffset(4, 4, 2, 2)
        };
        
        /// <summary>
        ///   <para>按钮样式。</para>
        /// </summary>
        private static readonly GUIStyle s_ButtonStyle = new GUIStyle("Button")
        {
            fixedHeight = 24,
            fixedWidth = 80,
            fontSize = 12,
            margin = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(6, 6, 2, 2)
        };
        
        /// <summary>
        ///   <para>显示单选弹窗。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="description">描述。</param>
        /// <param name="options">选项。</param>
        /// <param name="onSelectedCallback">选中回调。</param>
        /// <param name="selectedText">选中文本。</param>
        /// <param name="onCancelCallback">取消回调。</param>
        /// <param name="cancelText">取消文本。</param>
        public static void Show(string title, string description, string[] options, Action<int> onSelectedCallback, string selectedText = "Ok", Action onCancelCallback = null, string cancelText = "Cancel")
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Length == 0) throw new ArgumentException("至少需要一个选项。", nameof(options));
            var window = CreateInstance<SingleSelectionDialog>();
            window.titleContent = new GUIContent(title);
            window.m_Description = description;
            window.m_Options = options;
            window.m_SelectedIndex = 0;
            window.m_SelectedText = selectedText;
            window.m_OnSelected = onSelectedCallback;
            window.m_CancelText = cancelText;
            window.m_OnCancel = onCancelCallback;

            window.minSize = new Vector2(300, 150);
            window.maxSize = new Vector2(400, 200);
        
            window.ShowUtility();
        }
        
        /// <summary>
        ///   <para>绘制界面。</para>
        /// </summary>
        void OnGUI()
        {
            bool? confirmed = null;
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
                confirmed = false;
            }
            EditorGUILayout.Space(15);
            if (!string.IsNullOrEmpty(m_SelectedText) && GUILayout.Button(m_SelectedText, s_ButtonStyle))
            {
                confirmed = true;
            }
            
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
                        confirmed = true;
                        Event.current.Use();
                        break;
                    case KeyCode.Escape:
                        confirmed = false;
                        Event.current.Use();
                        break;
                }
            }
            if (!confirmed.HasValue) return;
            var selected = m_SelectedIndex;
            var onSelected = m_OnSelected;
            var onCancel = m_OnCancel;
            Close();
            if (confirmed.Value) onSelected?.Invoke(selected);
            else onCancel?.Invoke();
            GUIUtility.ExitGUI();
        }

        /// <summary>
        ///   <para>关闭时解除回调与选项引用。</para>
        /// </summary>
        private void OnDisable()
        {
            m_OnSelected = null;
            m_OnCancel = null;
            m_Options = null;
        }
    }
}

#endif