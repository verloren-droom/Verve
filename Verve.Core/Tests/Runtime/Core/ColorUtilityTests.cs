#if UNITY_5_3_OR_NEWER

namespace Verve.Tests.Core
{
    using NUnit.Framework;
    using UnityEngine;
    using Verve;

    [Category("Core")]
    internal sealed class ColorUtilityTests
    {
        [Test]
        public void LerpHSV_UsesShortestHuePathAndClampsRatio()
        {
            var from = Color.HSVToRGB(350f / 360f, 1f, 1f);
            var to = Color.HSVToRGB(10f / 360f, 1f, 1f);

            var midpoint = Game.ColorUtility.LerpHSV(from, to, 0.5f);
            Assert.That(midpoint.r, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(midpoint.g, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(midpoint.b, Is.EqualTo(0f).Within(0.0001f));
            AssertColor(Game.ColorUtility.LerpHSV(from, to, -1f), from);
            AssertColor(Game.ColorUtility.LerpHSV(from, to, 2f), to);
        }

        [Test]
        public void LerpHSV_UsesColoredHueForGrayInput()
        {
            var gray = new Color(0.5f, 0.5f, 0.5f, 0.25f);
            var blue = new Color(0f, 0f, 1f, 0.75f);

            var midpoint = Game.ColorUtility.LerpHSV(gray, blue, 0.5f);
            Assert.That(midpoint.r, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That(midpoint.g, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That(midpoint.b, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(midpoint.a, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void LerpHSVUnclamped_ExtrapolatesAlpha()
        {
            var from = new Color(1f, 0f, 0f, 0.2f);
            var to = new Color(0f, 1f, 0f, 0.8f);

            var result = Game.ColorUtility.LerpHSVUnclamped(from, to, 1.5f);

            Assert.That(result.a, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(result.r, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(result.g, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(result.b, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void PremultiplyAlpha_MultipliesRgbAndCanRoundTrip()
        {
            var source = new Color(0.8f, 0.4f, 0.2f, 0.25f);

            var premultiplied = Game.ColorUtility.PremultiplyAlpha(source);
            Assert.That(premultiplied.r, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(premultiplied.g, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(premultiplied.b, Is.EqualTo(0.05f).Within(0.0001f));
            AssertColor(Game.ColorUtility.UnpremultiplyAlpha(premultiplied), source);
        }

        [Test]
        public void UnpremultiplyAlpha_ZeroAlphaReturnsTransparentBlack()
        {
            var result = Game.ColorUtility.UnpremultiplyAlpha(new Color(1f, 0.5f, 0.25f, 0f));

            Assert.That(result, Is.EqualTo(new Color(0f, 0f, 0f, 0f)));
            Assert.That(Game.NumberUtility.IsFinite(result.r), Is.True);
            Assert.That(Game.NumberUtility.IsFinite(result.g), Is.True);
            Assert.That(Game.NumberUtility.IsFinite(result.b), Is.True);
        }

        [Test]
        public void AlphaBlend_CompositesForegroundOverBackground()
        {
            var background = new Color(0.2f, 0.4f, 0.8f, 0.5f);
            var foreground = new Color(1f, 0.5f, 0f, 0.25f);

            var result = Game.ColorUtility.AlphaBlend(background, foreground);

            Assert.That(result.r, Is.EqualTo(0.52f).Within(0.0001f));
            Assert.That(result.g, Is.EqualTo(0.44f).Within(0.0001f));
            Assert.That(result.b, Is.EqualTo(0.48f).Within(0.0001f));
            Assert.That(result.a, Is.EqualTo(0.625f).Within(0.0001f));
        }

        [Test]
        public void AlphaBlend_HandlesTransparentAndOpaqueBoundaries()
        {
            var background = new Color(0.2f, 0.4f, 0.8f, 0.5f);
            var foreground = new Color(1f, 0.5f, 0f, 1f);

            AssertColor(Game.ColorUtility.AlphaBlend(background, new Color(1f, 0f, 1f, 0f)), background);
            AssertColor(Game.ColorUtility.AlphaBlend(background, foreground), foreground);
            AssertColor(Game.ColorUtility.AlphaBlend(Color.clear, Color.clear), Color.clear);
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }
    }
}

#endif
