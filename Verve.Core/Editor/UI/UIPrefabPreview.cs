#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;

    /// <summary>
    ///   <para>UI 预制体预览；显示页面或部件的预制体内容。</para>
    /// </summary>
    [CustomPreview(typeof(GameObject))]
    internal sealed class UIPrefabPreview : ObjectPreview
    {
        /// <summary>
        ///   <para>摄像机近裁剪距离。</para>
        /// </summary>
        private const float CameraNearClip = 0.01f;
        /// <summary>
        ///   <para>摄像机内边距。</para>
        /// </summary>
        private const float CameraPadding = 0.6f;
        /// <summary>
        ///   <para>最大预览数量。</para>
        /// </summary>
        private const int MaximumPreviewCount = 25;
        /// <summary>
        ///   <para>棋盘格纹理尺寸。</para>
        /// </summary>
        private const int CheckerTextureSize = 16;
        /// <summary>
        ///   <para>棋盘格单元尺寸。</para>
        /// </summary>
        private const int CheckerCellSize = CheckerTextureSize / 2;

        /// <summary>
        ///   <para>属性编辑器类型。</para>
        /// </summary>
        private static readonly Type s_PropertyEditorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PropertyEditor");
        /// <summary>
        ///   <para>预览列表字段。</para>
        /// </summary>
        private static readonly FieldInfo s_PreviewListField = GetPropertyEditorField("m_Previews");
        /// <summary>
        ///   <para>选中预览字段。</para>
        /// </summary>
        private static readonly FieldInfo s_SelectedPreviewField = GetPropertyEditorField("m_SelectedPreview");
        /// <summary>
        ///   <para>跟踪器字段。</para>
        /// </summary>
        private static readonly FieldInfo s_TrackerField = GetPropertyEditorField("m_Tracker");
        /// <summary>
        ///   <para>预览。</para>
        /// </summary>
        private readonly Dictionary<GameObject, PreviewContext> m_Previews = new();
        /// <summary>
        ///   <para>棋盘格背景纹理。</para>
        /// </summary>
        private static Texture2D s_CheckerTexture;

        /// <summary>
        ///   <para>预览上下文。</para>
        /// </summary>
        private sealed class PreviewContext
        {
            /// <summary>
            ///   <para>渲染工具。</para>
            /// </summary>
            internal readonly PreviewRenderUtility RenderUtility;
            /// <summary>
            ///   <para>包围盒。</para>
            /// </summary>
            internal readonly Bounds Bounds;

            /// <summary>
            ///   <para>创建预览上下文。</para>
            /// </summary>
            /// <param name="renderUtility">渲染工具。</param>
            /// <param name="bounds">包围盒。</param>
            internal PreviewContext(PreviewRenderUtility renderUtility, Bounds bounds)
            {
                RenderUtility = renderUtility;
                Bounds = bounds;
            }
        }

        /// <summary>
        ///   <para>创建 UI 预制体预览。</para>
        /// </summary>
        static UIPrefabPreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseCheckerTexture;
            EditorApplication.quitting += ReleaseCheckerTexture;
        }

        /// <inheritdoc />
        public override void Initialize(Object[] targets)
        {
            ClearPreviews();
            base.Initialize(targets);
            try
            {
                CreatePreviews(targets);
                if (HasPreviewGUI()) TryActivatePreview(targets);
            }
            catch (Exception failure)
            {
                try { ClearPreviews(); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
        }

        /// <inheritdoc />
        public override bool HasPreviewGUI() => m_Previews.Count != 0;

        /// <inheritdoc />
        public override GUIContent GetPreviewTitle() => new GUIContent(m_Previews.Count > 1 ? $"UI Prefabs ({m_Previews.Count})" : "UI Prefab");

        /// <inheritdoc />
        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var prefab = target as GameObject;
            if (prefab == null || !m_Previews.TryGetValue(prefab, out var preview))
            {
                return;
            }

            FitCamera(preview, rect);
            preview.RenderUtility.BeginPreview(rect, GUIStyle.none);
            Exception failure = null;
            try { preview.RenderUtility.camera.Render(); }
            catch (Exception error) { failure = error; }

            Texture texture;
            // 每次 BeginPreview 只结束一次，同时保留渲染与清理错误。
            try { texture = preview.RenderUtility.EndPreview(); }
            catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
            ExceptionUtility.Rethrow(failure);
            if (texture == null) return;

            DrawCheckerBackground(CalculateContentRect(preview, rect));
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        }

        /// <inheritdoc />
        public override void Cleanup()
        {
            try { ClearPreviews(); }
            finally { base.Cleanup(); }
        }

        /// <summary>
        ///   <para>创建预览。</para>
        /// </summary>
        /// <param name="targets">目标。</param>
        private void CreatePreviews(Object[] targets)
        {
            if (targets == null)
            {
                return;
            }

            for (var i = 0; i < targets.Length && m_Previews.Count < MaximumPreviewCount; i++)
            {
                var prefab = targets[i] as GameObject;
                if (!IsUIPrefab(prefab) || m_Previews.ContainsKey(prefab))
                {
                    continue;
                }

                m_Previews.Add(prefab, CreatePreview(prefab));
            }
        }

        /// <summary>
        ///   <para>创建预览。</para>
        /// </summary>
        /// <param name="prefab">预制体。</param>
        private static PreviewContext CreatePreview(GameObject prefab)
        {
            var renderUtility = new PreviewRenderUtility();
            try
            {
                var instance = renderUtility.InstantiatePrefabInScene(prefab);
                instance.name = $"{prefab.name} (UI Preview)";
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.position = Vector3.zero;
                PrepareCanvases(instance);
                PrepareCamera(renderUtility);
                return new PreviewContext(renderUtility, CalculateBounds(instance));
            }
            catch (Exception failure)
            {
                try { renderUtility.Cleanup(); }
                catch (Exception cleanup) { throw ExceptionUtility.Combine(failure, cleanup); }
                throw;
            }
        }

        /// <summary>
        ///   <para>准备画布。</para>
        /// </summary>
        /// <param name="instance">实例。</param>
        private static void PrepareCanvases(GameObject instance)
        {
            var canvases = instance.GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
            {
                canvases = new[] { instance.AddComponent<Canvas>() };
            }

            for (var i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.WorldSpace;
                canvases[i].worldCamera = null;
            }
        }

        /// <summary>
        ///   <para>准备摄像机。</para>
        /// </summary>
        /// <param name="renderUtility">渲染工具。</param>
        private static void PrepareCamera(PreviewRenderUtility renderUtility)
        {
            renderUtility.camera.clearFlags = CameraClearFlags.Color;
            renderUtility.camera.backgroundColor = Color.clear;
            renderUtility.camera.orthographic = true;
            renderUtility.camera.nearClipPlane = CameraNearClip;
            renderUtility.camera.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        ///   <para>调整摄像机以完整显示预制体。</para>
        /// </summary>
        /// <param name="preview">预览。</param>
        /// <param name="rect">区域。</param>
        private static void FitCamera(PreviewContext preview, Rect rect)
        {
            var aspect = Mathf.Max(0.1f, rect.width / Mathf.Max(1f, rect.height));
            var width = Mathf.Max(0.01f, preview.Bounds.size.x);
            var height = Mathf.Max(0.01f, preview.Bounds.size.y);
            var size = Mathf.Max(height, width / aspect) * CameraPadding;
            var center = preview.Bounds.center;
            var camera = preview.RenderUtility.camera;
            camera.orthographicSize = size;
            var distance = Mathf.Max(1f, preview.Bounds.extents.z + 1f);
            camera.transform.position = new Vector3(center.x, center.y, center.z - distance);
            camera.farClipPlane = distance + preview.Bounds.extents.z + 1f;
            camera.transform.rotation = Quaternion.identity;
        }

        // 返回包围盒在预览相机视口中的像素区域，棋盘格只绘制在此范围。
        /// <summary>
        ///   <para>计算内容区域。</para>
        /// </summary>
        /// <param name="preview">预览。</param>
        /// <param name="previewRect">预览区域。</param>
        private static Rect CalculateContentRect(PreviewContext preview, Rect previewRect)
        {
            var aspect = Mathf.Max(0.1f, previewRect.width / Mathf.Max(1f, previewRect.height));
            var viewportWidth = Mathf.Max(0.01f, preview.RenderUtility.camera.orthographicSize * 2f * aspect);
            var viewportHeight = Mathf.Max(0.01f, preview.RenderUtility.camera.orthographicSize * 2f);
            var width = previewRect.width * Mathf.Clamp01(preview.Bounds.size.x / viewportWidth);
            var height = previewRect.height * Mathf.Clamp01(preview.Bounds.size.y / viewportHeight);
            var size = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
            return new Rect(
                previewRect.center.x - size.x * 0.5f,
                previewRect.center.y - size.y * 0.5f,
                size.x,
                size.y);
        }

        /// <summary>
        ///   <para>清空预览。</para>
        /// </summary>
        private void ClearPreviews()
        {
            var previews = new List<PreviewContext>(m_Previews.Values);
            m_Previews.Clear();
            // Cleanup 关闭预览场景，连同场景内实例一起释放。
            ResourceUtility.ReleaseAll(previews, preview => preview.RenderUtility.Cleanup());
        }

        // 当前实例加入列表前同步切换预览，避免先绘制一帧默认的 GameObject 预览。
        /// <summary>
        ///   <para>尝试激活预览。</para>
        /// </summary>
        /// <param name="previewTargets">预览目标。</param>
        private void TryActivatePreview(Object[] previewTargets)
        {
            if (s_PropertyEditorType == null || s_PreviewListField == null ||
                s_SelectedPreviewField == null || s_TrackerField == null)
            {
                return;
            }

            var inspectors = Resources.FindObjectsOfTypeAll(s_PropertyEditorType);
            for (var i = 0; i < inspectors.Length; i++)
            {
                var previews = s_PreviewListField.GetValue(inspectors[i]) as IList;
                var tracker = s_TrackerField.GetValue(inspectors[i]) as ActiveEditorTracker;
                if (previews == null || previews.Count != 0 || !TrackerMatchesTargets(tracker, previewTargets))
                {
                    continue;
                }

                s_SelectedPreviewField.SetValue(inspectors[i], this);
                return;
            }
        }

        /// <summary>
        ///   <para>跟踪器匹配目标。</para>
        /// </summary>
        /// <param name="tracker">跟踪器。</param>
        /// <param name="previewTargets">预览目标。</param>
        private static bool TrackerMatchesTargets(ActiveEditorTracker tracker, Object[] previewTargets)
        {
            if (tracker == null || previewTargets == null)
            {
                return false;
            }

            var editors = tracker.activeEditors;
            for (var i = 0; i < editors.Length; i++)
            {
                if (TargetsMatch(editors[i].targets, previewTargets))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>目标匹配。</para>
        /// </summary>
        /// <param name="first">首个。</param>
        /// <param name="second">第二个。</param>
        private static bool TargetsMatch(Object[] first, Object[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }

            for (var i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>获取属性编辑器字段。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        private static FieldInfo GetPropertyEditorField(string name) => s_PropertyEditorType == null ? null :
            Game.ReflectionUtility.FindField(s_PropertyEditorType, name, BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        ///   <para>绘制棋盘格背景。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        private static void DrawCheckerBackground(Rect rect)
        {
            var texture = GetCheckerTexture();
            var texCoords = new Rect(
                0f,
                0f,
                rect.width / CheckerTextureSize,
                rect.height / CheckerTextureSize);
            GUI.DrawTextureWithTexCoords(rect, texture, texCoords, true);
        }

        /// <summary>
        ///   <para>获取棋盘格纹理。</para>
        /// </summary>
        private static Texture2D GetCheckerTexture()
        {
            if (s_CheckerTexture != null)
            {
                return s_CheckerTexture;
            }

            var dark = EditorGUIUtility.isProSkin
                ? new Color32(57, 57, 57, 255)
                : new Color32(181, 181, 181, 255);
            var light = EditorGUIUtility.isProSkin
                ? new Color32(69, 69, 69, 255)
                : new Color32(199, 199, 199, 255);
            var pixels = new Color32[CheckerTextureSize * CheckerTextureSize];
            for (var y = 0; y < CheckerTextureSize; y++)
            {
                for (var x = 0; x < CheckerTextureSize; x++)
                {
                    var isDarkCell = ((x / CheckerCellSize) + (y / CheckerCellSize)) % 2 == 0;
                    pixels[(y * CheckerTextureSize) + x] = isDarkCell ? dark : light;
                }
            }

            s_CheckerTexture = new Texture2D(CheckerTextureSize, CheckerTextureSize, TextureFormat.RGBA32, false)
            {
                name = "UI Preview Checker",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            s_CheckerTexture.SetPixels32(pixels);
            s_CheckerTexture.Apply(false, true);
            return s_CheckerTexture;
        }

        /// <summary>
        ///   <para>释放棋盘格纹理。</para>
        /// </summary>
        private static void ReleaseCheckerTexture()
        {
            if (s_CheckerTexture == null)
            {
                return;
            }

            Object.DestroyImmediate(s_CheckerTexture);
            s_CheckerTexture = null;
        }

        /// <summary>
        ///   <para>计算包围盒。</para>
        /// </summary>
        /// <param name="root">根节点。</param>
        private static Bounds CalculateBounds(GameObject root)
        {
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            var hasBounds = false;
            var rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
            var corners = new Vector3[4];
            for (var i = 0; i < rectTransforms.Length; i++)
            {
                rectTransforms[i].GetWorldCorners(corners);
                for (var j = 0; j < corners.Length; j++)
                {
                    if (!hasBounds)
                    {
                        bounds = new Bounds(corners[j], Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(corners[j]);
                    }
                }
            }

            if (hasBounds)
            {
                return bounds;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return hasBounds ? bounds : new Bounds(root.transform.position, Vector3.one);
        }

        /// <summary>
        ///   <para>判断是否为 UI 预制体。</para>
        /// </summary>
        /// <param name="prefab">预制体。</param>
        private static bool IsUIPrefab(GameObject prefab)
        {
            if (prefab == null || !EditorUtility.IsPersistent(prefab))
            {
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return prefab.GetComponentInChildren<UIViewComponent>(true) != null ||
                   prefab.GetComponentInChildren<UIWidgetComponent>(true) != null;
        }
    }
}

#endif
