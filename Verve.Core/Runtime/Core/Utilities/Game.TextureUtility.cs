#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>纹理工具类。</para>
        /// </summary>
        public static class TextureUtility
        {
            /// <summary>
            ///   <para>创建纯色的2D 纹理。</para>
            /// </summary>
            /// <param name="width">2D 纹理的宽度。</param>
            /// <param name="height">2D 纹理的高度。</param>
            /// <param name="col">2D 纹理的颜色。</param>
            public static Texture2D MakeTex2D(int width, int height, Color col)
            {
                ValidateSize(width, height);
                var pix = new Color[width * height];
                for (var i = 0; i < pix.Length; i++)
                    pix[i] = col;
                var result = new Texture2D(width, height);
                result.SetPixels(pix);
                result.Apply();
                return result;
            }

            /// <summary>
            ///   <para>使用 8 位颜色通道创建纯色的 2D 纹理。</para>
            /// </summary>
            /// <param name="width">2D 纹理的宽度。</param>
            /// <param name="height">2D 纹理的高度。</param>
            /// <param name="col">2D 纹理的颜色。</param>
            /// <remarks>
            ///   <para>相比使用 <see cref="Color"/> 的重载，此方法直接写入 Color32，适合颜色已经来自像素数据的场景。</para>
            /// </remarks>
            public static Texture2D MakeTex2D(int width, int height, Color32 col)
            {
                ValidateSize(width, height);
                var pixels = new Color32[width * height];
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i] = col;
                var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.SetPixels32(pixels);
                result.Apply(false, false);
                return result;
            }

            /// <summary>
            ///   <para>翻转可读的 2D 纹理。</para>
            /// </summary>
            /// <param name="texture">待翻转的纹理。</param>
            /// <param name="horizontal">是否沿水平方向翻转。</param>
            /// <param name="vertical">是否沿垂直方向翻转。</param>
            /// <param name="updateMipmaps">是否重新生成 Mipmap。</param>
            /// <remarks>
            ///   <para>此操作会从纹理读取像素并创建一个临时 Color32 数组；纹理必须启用 Read/Write。</para>
            /// </remarks>
            public static void Flip(
                Texture2D texture,
                bool horizontal = true,
                bool vertical = false,
                bool updateMipmaps = true)
            {
                if (texture == null) throw new ArgumentNullException(nameof(texture));
                if (!horizontal && !vertical) return;

                var width = texture.width;
                var height = texture.height;
                var pixels = texture.GetPixels32();
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var sourceX = horizontal ? width - x - 1 : x;
                        var sourceY = vertical ? height - y - 1 : y;
                        var sourceIndex = sourceY * width + sourceX;
                        var targetIndex = y * width + x;
                        if (sourceIndex <= targetIndex) continue;
                        (pixels[targetIndex], pixels[sourceIndex]) = (pixels[sourceIndex], pixels[targetIndex]);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(updateMipmaps, false);
            }

            /// <summary>
            ///   <para>创建可读的 90 度旋转纹理。</para>
            /// </summary>
            /// <param name="source">源纹理。</param>
            /// <param name="clockwise">是否顺时针旋转；传入 false 时逆时针旋转。</param>
            /// <returns>新的 RGBA32 纹理；源纹理保持不变。</returns>
            /// <remarks>
            ///   <para>此操作会读取源纹理并分配新的像素数组，源纹理必须启用 Read/Write。</para>
            /// </remarks>
            public static Texture2D Rotate90(Texture2D source, bool clockwise = true)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));

                var sourceWidth = source.width;
                var sourceHeight = source.height;
                var sourcePixels = source.GetPixels32();
                var target = new Texture2D(sourceHeight, sourceWidth, TextureFormat.RGBA32, false);
                var targetPixels = new Color32[sourcePixels.Length];
                for (var y = 0; y < sourceHeight; y++)
                {
                    for (var x = 0; x < sourceWidth; x++)
                    {
                        var targetX = clockwise ? sourceHeight - y - 1 : y;
                        var targetY = clockwise ? x : sourceWidth - x - 1;
                        targetPixels[targetY * sourceHeight + targetX] = sourcePixels[y * sourceWidth + x];
                    }
                }

                target.SetPixels32(targetPixels);
                target.Apply(false, false);
                target.filterMode = source.filterMode;
                target.wrapMode = source.wrapMode;
                return target;
            }

            private static void ValidateSize(int width, int height)
            {
                if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "纹理宽度必须大于零。");
                if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "纹理高度必须大于零。");
                if ((long)width * height > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(width), "纹理像素数量超出可分配范围。");
            }
        }
    }
}

#endif