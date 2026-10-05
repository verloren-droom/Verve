#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;
    
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>颜色工具类。</para>
        /// </summary>
        public static class ColorUtility
        {
            private const float HueEpsilon = 0.00001f;

            /// <summary>
            ///   <para>在 HSV 色彩空间中沿色相最短路径插值。</para>
            /// </summary>
            /// <param name="from">起始颜色。</param>
            /// <param name="to">目标颜色。</param>
            /// <param name="t">插值比例；会限制在 0 到 1 之间。</param>
            /// <remarks>
            ///   <para>与 <see cref="UnityEngine.Color.Lerp(Color, Color, float)"/> 不同，此方法不会在 RGB 空间中插值，因此适合颜色渐变和色相动画。</para>
            ///   <para>灰度颜色没有有意义的色相；与有色颜色插值时会沿用有色一侧的色相，避免经过无关的红色色相。</para>
            /// </remarks>
            public static Color LerpHSV(Color from, Color to, float t)
                => LerpHSVUnclamped(from, to, Mathf.Clamp01(t));

            /// <summary>
            ///   <para>在 HSV 色彩空间中沿色相最短路径进行非限制插值。</para>
            /// </summary>
            /// <param name="from">起始颜色。</param>
            /// <param name="to">目标颜色。</param>
            /// <param name="t">插值比例。</param>
            public static Color LerpHSVUnclamped(Color from, Color to, float t)
            {
                Color.RGBToHSV(from, out var fromHue, out var fromSaturation, out var fromValue);
                Color.RGBToHSV(to, out var toHue, out var toSaturation, out var toValue);

                if (fromSaturation <= HueEpsilon && toSaturation > HueEpsilon)
                    fromHue = toHue;
                else if (toSaturation <= HueEpsilon && fromSaturation > HueEpsilon)
                    toHue = fromHue;

                var hueDelta = toHue - fromHue;
                if (hueDelta > 0.5f) hueDelta -= 1f;
                else if (hueDelta < -0.5f) hueDelta += 1f;

                var hue = fromHue + hueDelta * t;
                hue -= Mathf.Floor(hue);
                var saturation = fromSaturation + (toSaturation - fromSaturation) * t;
                var value = fromValue + (toValue - fromValue) * t;
                var result = Color.HSVToRGB(hue, saturation, value, true);
                result.a = from.a + (to.a - from.a) * t;
                return result;
            }

            /// <summary>
            ///   <para>将颜色转换为预乘 Alpha 形式。</para>
            /// </summary>
            /// <param name="color">源颜色。</param>
            /// <returns>RGB 通道已乘以 Alpha 的颜色。</returns>
            public static Color PremultiplyAlpha(Color color)
                => new Color(color.r * color.a, color.g * color.a, color.b * color.a, color.a);

            /// <summary>
            ///   <para>将预乘 Alpha 颜色还原为普通 Alpha 形式。</para>
            /// </summary>
            /// <param name="color">预乘 Alpha 颜色。</param>
            /// <returns>RGB 通道已除以 Alpha 的颜色；Alpha 为零时返回透明黑色，避免产生 NaN。</returns>
            public static Color UnpremultiplyAlpha(Color color)
            {
                if (color.a == 0f) return new Color(0f, 0f, 0f, color.a);
                var inverseAlpha = 1f / color.a;
                return new Color(color.r * inverseAlpha, color.g * inverseAlpha, color.b * inverseAlpha, color.a);
            }

            /// <summary>
            ///   <para>按“前景覆盖背景”规则合成两个颜色。</para>
            /// </summary>
            /// <param name="background">背景颜色。</param>
            /// <param name="foreground">前景颜色。</param>
            public static Color AlphaBlend(Color background, Color foreground)
            {
                var foregroundAlpha = foreground.a;
                var backgroundAlpha = background.a;
                var outputAlpha = foregroundAlpha + backgroundAlpha * (1f - foregroundAlpha);
                if (outputAlpha <= 0f) return new Color(0f, 0f, 0f, outputAlpha);

                var backgroundWeight = backgroundAlpha * (1f - foregroundAlpha);
                var inverseOutputAlpha = 1f / outputAlpha;
                return new Color(
                    (foreground.r * foregroundAlpha + background.r * backgroundWeight) * inverseOutputAlpha,
                    (foreground.g * foregroundAlpha + background.g * backgroundWeight) * inverseOutputAlpha,
                    (foreground.b * foregroundAlpha + background.b * backgroundWeight) * inverseOutputAlpha,
                    outputAlpha);
            }
        }
    }
}

#endif