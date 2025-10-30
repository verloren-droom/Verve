#if UNITY_EDITOR

namespace VerveEditor
{
    using UnityEngine;
    using UnityEditor;
    
    
    /// <summary>
    ///   <para>页签翻页控件</para>
    /// </summary>
    public sealed class TabPagerBox
    {
        private int    m_Selected;
        private Vector2 m_Scroll;

        
        private static class Styles
        {
            public static GUIStyle Box { get; } = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 6, 6),
                margin  = new RectOffset(4, 4, 4, 4)
            };
            public static GUIStyle Tab { get; } = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedHeight = 28,
                fontSize    = 12,
                alignment   = TextAnchor.MiddleCenter,
                margin      = new RectOffset(0, 0, 0, 0)
            };
            public static GUIStyle TabActive { get; } = new GUIStyle(Tab)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
            public static GUIStyle Indicator { get; } = new GUIStyle
            {
                normal = { background = Texture2D.whiteTexture },
                fixedHeight = 2
            };
        }


        public TabPagerBox(int startPage = 0)
        {
            m_Selected = startPage;
        }

        /// <summary>
        ///   <para>开始绘制，返回新选中的索引</para>
        /// </summary>
        public int Begin(params GUIContent[] titles)
        {
            if (titles == null || titles.Length == 0) return m_Selected;
    
            GUILayout.BeginVertical(Styles.Box);
            float totalW = 0;
            foreach (var t in titles)
                totalW += Mathf.Max(80, Styles.Tab.CalcSize(t).x + 20);
    
            Rect lineRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                                                    GUILayout.Height(30));
            Rect viewRect = new Rect(0, 0, totalW, 30);
            m_Scroll = GUI.BeginScrollView(lineRect, m_Scroll, viewRect, false, false,
                                         GUIStyle.none, GUIStyle.none);
    
            float x = 0;
            for (int i = 0; i < titles.Length; i++)
            {
                float w = Mathf.Max(80, Styles.Tab.CalcSize(titles[i]).x + 20);
                Rect r = new Rect(x, 0, w, 28);
                bool on = GUI.Toggle(r, m_Selected == i, titles[i],
                                     m_Selected == i ? Styles.TabActive : Styles.Tab);
                if (on && m_Selected != i) m_Selected = i;
                x += w;
            }
            GUI.EndScrollView();
    

            float indX = 0;
            for (int i = 0; i < m_Selected; i++)
                indX += Mathf.Max(80, Styles.Tab.CalcSize(titles[i]).x + 20);
            float indW = Mathf.Max(80, Styles.Tab.CalcSize(titles[m_Selected]).x + 20);
            Rect indR = new Rect(lineRect.x + indX - m_Scroll.x, lineRect.yMax - 2, indW, 2);
            GUI.Box(indR, GUIContent.none, Styles.Indicator);
    
            GUILayout.BeginVertical(GUILayout.MinHeight(100));
            return m_Selected;
        }
        
        /// <summary>
        ///   <para>开始绘制，返回新选中的索引</para>
        /// </summary>
        public int Begin(params string[] titles) => Begin(System.Array.ConvertAll(titles, x => new GUIContent(x)));
    
        /// <summary>
        ///   <para>结束包围框</para>
        /// </summary>
        public void End()
        {
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }
    }
}

#endif