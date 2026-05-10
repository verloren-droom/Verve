#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>纹理工具类</para>
        /// </summary>
        public static class TextureUtility
        {
            /// <summary>
            ///   <para>创建纯色的2D纹理</para>
            /// </summary>
            /// <param name="width">2D纹理的宽度</param>
            /// <param name="height">2D纹理的高度</param>
            /// <param name="col">2D纹理的颜色</param>
            public static Texture2D MakeTex2D(int width, int height, Color col)
            {
                var pix = new Color[width * height];
                for (var i = 0; i < pix.Length; i++)
                    pix[i] = col;
                var result = new Texture2D(width, height);
                result.SetPixels(pix);
                result.Apply();
                return result;
            }
        }
    }
}

#endif
