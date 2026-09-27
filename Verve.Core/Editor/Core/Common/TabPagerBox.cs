#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEngine;
    using UnityEditor;
    
    /// <summary>
    ///   <para>页签翻页控件。</para>
    /// </summary>
    public sealed class TabPagerBox
    {
        /// <summary>
        ///   <para>选中。</para>
        /// </summary>
        private int m_Selected;
        /// <summary>
        ///   <para>滚动。</para>
        /// </summary>
        private Vector2 m_Scroll;
        /// <summary>
        ///   <para>页签宽度缓存。</para>
        /// </summary>
        private float[] m_TabWidths = Array.Empty<float>();

        /// <summary>
        ///   <para>绘制样式。</para>
        /// </summary>
        private static class Styles
        {
            /// <summary>
            ///   <para>容器。</para>
            /// </summary>
            public static GUIStyle Box { get; } = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 6, 6),
                margin  = new RectOffset(4, 4, 4, 4)
            };
            /// <summary>
            ///   <para>标签页。</para>
            /// </summary>
            public static GUIStyle Tab { get; } = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedHeight = 28,
                fontSize    = 12,
                alignment   = TextAnchor.MiddleCenter,
                margin      = new RectOffset(0, 0, 0, 0)
            };
            /// <summary>
            ///   <para>标签页激活。</para>
            /// </summary>
            public static GUIStyle TabActive { get; } = new GUIStyle(Tab)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

        }


        /// <summary>
        ///   <para>创建页签翻页控件。</para>
        /// </summary>
        /// <param name="startPage">开始页。</param>
        public TabPagerBox(int startPage = 0) => m_Selected = startPage;

        /// <summary>
        ///   <para>开始绘制，返回新选中的索引。</para>
        /// </summary>
        /// <param name="titles">标题。</param>
        public int Begin(params GUIContent[] titles)
        {
            if (titles == null) throw new ArgumentNullException(nameof(titles));
            if (titles.Length == 0) throw new ArgumentException("至少需要一个页签。", nameof(titles));
            m_Selected = Mathf.Clamp(m_Selected, 0, titles.Length - 1);
            if (m_TabWidths.Length != titles.Length) Array.Resize(ref m_TabWidths, titles.Length);
            float totalWidth = 0;
            for (int i = 0; i < titles.Length; i++)
                totalWidth += m_TabWidths[i] = Mathf.Max(80, Styles.Tab.CalcSize(titles[i]).x + 20);

            GUILayout.BeginVertical(Styles.Box);
    
            Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                                                    GUILayout.Height(30));
            Rect viewRect = new Rect(0, 0, totalWidth, 30);
            m_Scroll = GUI.BeginScrollView(lineRect, m_Scroll, viewRect, false, false,
                                         GUIStyle.none, GUIStyle.none);
    
            float x = 0;
            for (int i = 0; i < titles.Length; i++)
            {
                float w = m_TabWidths[i];
                Rect r = new Rect(x, 0, w, 28);
                bool on = GUI.Toggle(r, m_Selected == i, titles[i],
                                     m_Selected == i ? Styles.TabActive : Styles.Tab);
                if (on && m_Selected != i) m_Selected = i;
                if (m_Selected == i)
                    EditorGUI.DrawRect(new Rect(x, 28, w, 2), Color.white);
                x += w;
            }
            GUI.EndScrollView();


            GUILayout.BeginVertical(GUILayout.MinHeight(100));
            return m_Selected;
        }
        
        /// <summary>
        ///   <para>开始绘制，返回新选中的索引。</para>
        /// </summary>
        /// <param name="titles">标题。</param>
        public int Begin(params string[] titles) => Begin(System.Array.ConvertAll(titles, x => new GUIContent(x)));
    
        /// <summary>
        ///   <para>结束包围框。</para>
        /// </summary>
        public void End()
        {
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }
    }
}

#endif