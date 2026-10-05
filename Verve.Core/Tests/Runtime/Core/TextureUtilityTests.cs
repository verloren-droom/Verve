#if UNITY_5_3_OR_NEWER

namespace Verve.Tests.Core
{
    using NUnit.Framework;
    using UnityEngine;
    using Verve;

    [Category("Core")]
    internal sealed class TextureUtilityTests
    {
        private static readonly Color32 A = new Color32(255, 0, 0, 255);
        private static readonly Color32 B = new Color32(0, 255, 0, 255);
        private static readonly Color32 C = new Color32(0, 0, 255, 255);
        private static readonly Color32 D = new Color32(255, 255, 0, 255);
        private static readonly Color32 E = new Color32(255, 0, 255, 255);
        private static readonly Color32 F = new Color32(0, 255, 255, 255);

        [Test]
        public void MakeTex2D_CreatesRequestedSolidTextures()
        {
            var colorTexture = Game.TextureUtility.MakeTex2D(2, 3, new Color(0.25f, 0.5f, 0.75f, 1f));
            var color32Texture = Game.TextureUtility.MakeTex2D(2, 3, new Color32(32, 64, 128, 255));
            try
            {
                Assert.That(colorTexture.width, Is.EqualTo(2));
                Assert.That(colorTexture.height, Is.EqualTo(3));
                Assert.That(colorTexture.GetPixel(1, 2).r, Is.EqualTo(0.25f).Within(0.01f));
                Assert.That(colorTexture.GetPixel(1, 2).g, Is.EqualTo(0.5f).Within(0.01f));
                Assert.That(colorTexture.GetPixel(1, 2).b, Is.EqualTo(0.75f).Within(0.01f));
                var color32Pixel = color32Texture.GetPixel(0, 0);
                Assert.That(color32Pixel.r, Is.EqualTo(32f / 255f).Within(0.0001f));
                Assert.That(color32Pixel.g, Is.EqualTo(64f / 255f).Within(0.0001f));
                Assert.That(color32Pixel.b, Is.EqualTo(128f / 255f).Within(0.0001f));
                Assert.That(color32Pixel.a, Is.EqualTo(1f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(colorTexture);
                Object.DestroyImmediate(color32Texture);
            }
        }

        [Test]
        public void MakeTex2D_RejectsNonPositiveDimensions()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Game.TextureUtility.MakeTex2D(0, 1, Color.white));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Game.TextureUtility.MakeTex2D(1, -1, Color.white));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Game.TextureUtility.MakeTex2D(0, 1, new Color32(255, 255, 255, 255)));
        }

        [Test]
        public void Flip_ReversesPixelsInRequestedDirections()
        {
            var horizontal = CreateSource();
            var vertical = CreateSource();
            try
            {
                Game.TextureUtility.Flip(horizontal, horizontal: true, vertical: false, updateMipmaps: false);
                CollectionAssert.AreEqual(new[] { B, A, D, C, F, E }, horizontal.GetPixels32());

                Game.TextureUtility.Flip(vertical, horizontal: false, vertical: true, updateMipmaps: false);
                CollectionAssert.AreEqual(new[] { E, F, C, D, A, B }, vertical.GetPixels32());
            }
            finally
            {
                Object.DestroyImmediate(horizontal);
                Object.DestroyImmediate(vertical);
            }
        }

        [Test]
        public void Rotate90_CreatesClockwiseAndCounterClockwiseCopies()
        {
            var source = CreateSource();
            var clockwise = Game.TextureUtility.Rotate90(source);
            var counterClockwise = Game.TextureUtility.Rotate90(source, clockwise: false);
            try
            {
                CollectionAssert.AreEqual(new[] { E, C, A, F, D, B }, clockwise.GetPixels32());
                CollectionAssert.AreEqual(new[] { B, D, F, A, C, E }, counterClockwise.GetPixels32());
                CollectionAssert.AreEqual(new[] { A, B, C, D, E, F }, source.GetPixels32());
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(clockwise);
                Object.DestroyImmediate(counterClockwise);
            }
        }

        private static Texture2D CreateSource()
        {
            var texture = new Texture2D(2, 3, TextureFormat.RGBA32, false);
            texture.SetPixels32(new[] { A, B, C, D, E, F });
            texture.Apply(false, false);
            return texture;
        }
    }
}

#endif
