#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Text;
    using System.Reflection;
    using System.Linq;
    using System.Collections.Generic;
    using System.Security;
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.SceneManagement;
    using Object = UnityEngine.Object;


    /// <summary>
    ///   <para>编辑器工具；共用绘制、资源写入和脚本查询。</para>
    /// </summary>
    public static partial class CoreEditorUtility
    {
        /// <summary>
        ///   <para>模块项目设置根路径；各模块通过 <see cref="SettingsProvider"/> 注册子页面。</para>
        /// </summary>
        public const string ModuleSettingsRoot = "Project/Verve/Modules";

        /// <summary>
        ///   <para>生成反射类型的裁剪保留配置。</para>
        /// </summary>
        /// <param name="types">需保留的类型；空项忽略，相同类型合并。</param>
        public static string CreateLinkXml(IEnumerable<Type> types)
        {
            var output = new StringBuilder("<linker>\n");
            foreach (var group in types.Where(type => type != null).Distinct()
                         .OrderBy(type => type.FullName, StringComparer.Ordinal)
                         .GroupBy(type => type.Assembly.GetName().Name).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                output.Append("  <assembly fullname=\"").Append(SecurityElement.Escape(group.Key)).AppendLine("\">");
                foreach (var type in group)
                    output.Append("    <type fullname=\"").Append(SecurityElement.Escape(type.FullName.Replace('+', '/')))
                        .AppendLine("\" preserve=\"all\" />");
                output.AppendLine("  </assembly>");
            }
            return output.AppendLine("</linker>").ToString();
        }

        /// <summary>
        ///   <para>内容变化时原子写入文本资源并导入。</para>
        /// </summary>
        /// <param name="assetPath">Assets 或 Packages 下的资源路径。</param>
        /// <param name="contents">文本内容。</param>
        /// <param name="encoding">编码。</param>
        /// <param name="importOptions">导入选项。</param>
        /// <returns>是否写入并导入了资源。</returns>
        public static bool WriteTextAsset(string assetPath, string contents, Encoding encoding,
            ImportAssetOptions importOptions = ImportAssetOptions.ForceUpdate)
        {
            assetPath = Game.PathUtility.NormalizeProjectPath(assetPath);
            if (!Game.FileUtility.WriteAllTextAtomically(assetPath, contents, encoding)) return false;
            AssetDatabase.ImportAsset(assetPath, importOptions);
            return true;
        }

        /// <summary>
        ///   <para>绘制按钮。</para>
        /// </summary>
        /// <param name="target">目标。</param>
        public static void DrawButtons(UnityEngine.Object target)
        {
            foreach (var method in target.GetType().GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>(false);
                if (buttonAttribute == null) continue;
                string buttonLabel = string.IsNullOrEmpty(buttonAttribute.Label) ? method.Name : buttonAttribute.Label;

                var parameters = method.GetParameters();

                if (parameters.Length > 0)
                {
                    if (buttonAttribute.Args == null || buttonAttribute.Args.Length != parameters.Length)
                    {
                        EditorGUILayout.HelpBox(
                            $"Method '{method.Name}' has parameters but ButtonAttribute does not specify matching parameters.",
                            MessageType.Warning);
                        continue;
                    }

                    if (!GUILayout.Button(buttonLabel)) continue;

                    object[] paramValues = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        string paramName = buttonAttribute.Args[i];
                        var paramType = parameters[i].ParameterType;

                        var field = Game.ReflectionUtility.FindField(target.GetType(), paramName);
                        if (field != null && field.FieldType == paramType)
                        {
                            paramValues[i] = field.GetValue(target);
                            continue;
                        }

                        var prop = Game.ReflectionUtility.FindProperty(target.GetType(), paramName);
                        if (prop != null && prop.PropertyType == paramType)
                        {
                            paramValues[i] = prop.GetValue(target);
                            continue;
                        }

                        var methodInfo = target.GetType().GetMethod(paramName,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                        if (methodInfo != null && methodInfo.ReturnType == paramType)
                        {
                            paramValues[i] = methodInfo.Invoke(target, null);
                            continue;
                        }

                        throw new InvalidOperationException($"Cannot resolve parameter '{paramName}' of method '{method.Name}'.");
                    }

                    method.Invoke(target, paramValues);
                }
                else
                {
                    if (GUILayout.Button(buttonLabel))
                    {
                        method.Invoke(target, null);
                    }
                }
            }
        }

        /// <summary>
        ///   <para>绘制交替行和悬停状态。</para>
        /// </summary>
        /// <param name="rowRect">行区域。</param>
        /// <param name="isHovering">是否悬停。</param>
        /// <param name="rowIndex">记录索引。</param>
        public static void DrawRowBackground(Rect rowRect, bool isHovering, int rowIndex)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var isProSkin = EditorGUIUtility.isProSkin;
            var baseLevel = isProSkin ? 0.22f : 0.92f;
            var alternateOffset = isProSkin ? 0.015f : -0.025f;
            var level = Mathf.Clamp01(baseLevel + (rowIndex & 1) * alternateOffset);
            var background = isHovering
                ? isProSkin
                    ? new Color(0.24f, 0.48f, 0.76f, 0.9f)
                    : new Color(0.32f, 0.55f, 0.86f, 0.9f)
                : new Color(level, level, level, 1f);

            EditorGUI.DrawRect(rowRect, background);
            EditorGUI.DrawRect(
                new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f),
                isProSkin
                    ? new Color(1f, 1f, 1f, 0.08f)
                    : new Color(0f, 0f, 0f, 0.12f));
        }

        /// <summary>
        ///   <para>取得场景实例对应的源 Prefab；资源对象和 Prefab Mode 对象不返回。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        public static GameObject GetSourcePrefabAsset(Component component)
        {
            if (component == null || component.gameObject == null ||
                EditorUtility.IsPersistent(component.gameObject) ||
                PrefabStageUtility.GetPrefabStage(component.gameObject) != null ||
                !component.gameObject.scene.IsValid())
            {
                return null;
            }

            var source = PrefabUtility.GetCorrespondingObjectFromSource(component.gameObject) as GameObject;
            if (source == null)
            {
                source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(component.gameObject) as GameObject;
            }

            var prefabPath = source == null ? null : AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject);
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                return null;
            }

            prefabPath = Game.PathUtility.Normalize(prefabPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        /// <summary>
        ///   <para>版本 Badge 样式。</para>
        /// </summary>
        public static GUIStyle VersionBadgeStyle
        {
            get
            {
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(6, 6, 1, 1),
                    margin = new RectOffset(2, 2, 2, 2),
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                    fixedHeight = 16
                };

                style.normal.background = null;

                return style;
            }
        }

        /// <summary>
        ///   <para>绘制分割线。</para>
        /// </summary>
        /// <param name="isBoxed">是否绘制边框。</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        /// <summary>
        ///   <para>绘制带开关、四边边框的头部。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="property">折叠属性；序列化更新与提交由调用方负责。</param>
        /// <param name="activeProperty">激活属性；可为空。</param>
        /// <param name="contextClickCallback">右键点击回调。</param>
        /// <param name="version">版本。</param>
        public static bool DrawHeaderToggle(
            GUIContent title,
            SerializedProperty property,
            SerializedProperty activeProperty = null,
            GenericMenu.MenuFunction2 contextClickCallback = null,
            string version = null
            )
        {
            var expanded = property.isExpanded;
            var enabled = activeProperty == null || activeProperty.boolValue;
            var result = DrawHeaderToggle(
                title,
                ref expanded,
                ref enabled,
                contextClickCallback == null
                    ? null
                    : position => contextClickCallback(position),
                version,
                activeProperty != null);
            property.isExpanded = expanded;
            if (activeProperty != null)
                activeProperty.boolValue = enabled;
            return result;
        }

        /// <summary>
        ///   <para>绘制普通折叠栏。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="expanded">折叠状态。</param>
        /// <param name="contextClickCallback">右侧菜单回调。</param>
        /// <returns>当前是否展开。</returns>
        public static bool DrawHeaderToggle(
            GUIContent title,
            ref bool expanded,
            Action<Vector2> contextClickCallback = null)
        {
            var enabled = true;
            return DrawHeaderToggle(title, ref expanded, ref enabled, contextClickCallback, null, false);
        }

        /// <summary>
        ///   <para>绘制带启用开关的折叠栏。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="expanded">折叠状态。</param>
        /// <param name="enabled">启用状态。</param>
        /// <param name="contextClickCallback">右侧菜单回调。</param>
        /// <param name="version">版本文本。</param>
        /// <returns>当前是否展开。</returns>
        public static bool DrawHeaderToggle(
            GUIContent title,
            ref bool expanded,
            ref bool enabled,
            Action<Vector2> contextClickCallback = null,
            string version = null)
            => DrawHeaderToggle(title, ref expanded, ref enabled, contextClickCallback, version, true);

        /// <summary>
        ///   <para>绘制带启用开关的折叠栏。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="expanded">折叠状态。</param>
        /// <param name="enabled">启用状态。</param>
        /// <param name="contextClickCallback">右侧菜单回调。</param>
        /// <param name="version">版本文本。</param>
        /// <param name="showToggle">是否显示启用开关。</param>
        /// <returns>当前是否展开。</returns>
        private static bool DrawHeaderToggle(
            GUIContent title,
            ref bool expanded,
            ref bool enabled,
            Action<Vector2> contextClickCallback,
            string version,
            bool showToggle)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));

            var backgroundRect = GUILayoutUtility.GetRect(1f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;
            var background = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.72f, 0.72f, 0.72f, 1f);
            var border = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.14f)
                : new Color(0f, 0f, 0f, 0.2f);
            EditorGUI.DrawRect(backgroundRect, background);
            EditorGUI.DrawRect(new Rect(backgroundRect.x, backgroundRect.y, backgroundRect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(backgroundRect.x, backgroundRect.yMax - 1f, backgroundRect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(backgroundRect.x, backgroundRect.y, 1f, backgroundRect.height), border);
            EditorGUI.DrawRect(new Rect(backgroundRect.xMax - 1f, backgroundRect.y, 1f, backgroundRect.height), border);

            // 原生折叠箭头和复选框的图标内边距不同，补偿差值后保持视觉位置一致。
            var foldoutRect = new Rect(backgroundRect.x + 5f, backgroundRect.y + 1f, 14f, 15f);
            var toggleRect = foldoutRect;
            toggleRect.x += EditorStyles.toggle.padding.left - EditorStyles.foldout.padding.left;
            var controlRect = Rect.MinMaxRect(
                Mathf.Min(foldoutRect.xMin, toggleRect.xMin),
                foldoutRect.yMin,
                Mathf.Max(foldoutRect.xMax, toggleRect.xMax),
                foldoutRect.yMax);
            const float labelX = 34f;
            var menuRect = new Rect(backgroundRect.xMax - 20f, backgroundRect.y, 18f, backgroundRect.height);
            var labelRect = new Rect(
                backgroundRect.x + labelX,
                backgroundRect.y,
                Mathf.Max(0f, menuRect.x - backgroundRect.x - labelX),
                backgroundRect.height);

            if (showToggle)
                enabled = GUI.Toggle(toggleRect, enabled, GUIContent.none, EditorStyles.toggle);
            else
                expanded = GUI.Toggle(foldoutRect, expanded, GUIContent.none, EditorStyles.foldout);
            using (new EditorGUI.DisabledScope(showToggle && !enabled))
                GUI.Label(labelRect, title, EditorStyles.label);

            if (!string.IsNullOrEmpty(version))
            {
                var badge = new GUIContent($"v{version}");
                var badgeSize = VersionBadgeStyle.CalcSize(badge);
                GUI.Label(
                    new Rect(labelRect.xMax - badgeSize.x, labelRect.y + (labelRect.height - badgeSize.y) * .5f,
                        badgeSize.x, badgeSize.y),
                    badge,
                    VersionBadgeStyle);
            }

            if (contextClickCallback != null &&
                GUI.Button(menuRect, EditorGUIUtility.IconContent("_Menu"), new GUIStyle("IconButton")))
                contextClickCallback(menuRect.position);

            var currentEvent = Event.current;
            if (GUI.enabled && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 &&
                backgroundRect.Contains(currentEvent.mousePosition) &&
                !controlRect.Contains(currentEvent.mousePosition) &&
                !menuRect.Contains(currentEvent.mousePosition))
            {
                expanded = !expanded;
                currentEvent.Use();
            }

            return expanded;
        }

        /// <summary>
        ///   <para>创建编辑器上下文菜单。</para>
        /// </summary>
        /// <param name="menuItems">菜单项。</param>
        public static GenericMenu CreateContextMenu(
            (string, GenericMenu.MenuFunction2)[] menuItems)
        {
            var menu = new GenericMenu();

            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                menu.AddItem(new GUIContent(item.Item1), false, item.Item2, item);
            }

            return menu;
        }

        /// <summary>
        ///   <para>创建上下文菜单。</para>
        /// </summary>
        /// <param name="menuItems">菜单项。</param>
        public static GenericMenu CreateContextMenu(
            (GUIContent, GenericMenu.MenuFunction2)[] menuItems)
        {
            var menu = new GenericMenu();

            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                menu.AddItem(item.Item1, false, item.Item2, item);
            }

            return menu;
        }

        /// <summary>
        ///   <para>标记对象为脏；持久资源仅保存其所属文件。</para>
        /// </summary>
        /// <param name="obj">对象。</param>
        public static void MarkDirtyAndSave(Object obj)
        {
            EditorUtility.SetDirty(obj);

            if (EditorUtility.IsPersistent(obj))
            {
                AssetDatabase.SaveAssetIfDirty(obj);
            }
        }

        /// <summary>
        ///   <para>紧凑标签按钮。</para>
        /// </summary>
        private static GUIStyle m_MiniLabelButton;

        /// <summary>
        ///   <para>小标签按钮样式。</para>
        /// </summary>
        public static GUIStyle MiniLabelButton
        {
            get
            {
                if (m_MiniLabelButton == null)
                {
                    m_MiniLabelButton = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = new GUIStyleState
                        {
                            scaledBackgrounds = null,
                            textColor = Color.grey
                        }
                    };
                    var activeState = new GUIStyleState
                    {
                        scaledBackgrounds = null,
                        textColor = Color.white
                    };
                    m_MiniLabelButton.active = activeState;
                    m_MiniLabelButton.onNormal = activeState;
                    m_MiniLabelButton.onActive = activeState;
                    return m_MiniLabelButton;
                }

                return m_MiniLabelButton;
            }
        }

        /// <summary>
        ///   <para>获取新对象的父节点；Prefab Mode 中使用当前 Prefab 根节点。</para>
        /// </summary>
        /// <param name="menuCommand">菜单命令。</param>
        public static GameObject GetParentObject(MenuCommand menuCommand)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null ? stage.prefabContentsRoot : GetCommandGameObject(menuCommand);
        }

        /// <summary>
        ///   <para>取得菜单指向的节点；没有对象上下文时使用当前选中节点。</para>
        /// </summary>
        /// <param name="command">菜单命令；可为 null。</param>
        public static GameObject GetCommandGameObject(MenuCommand command) => command?.context switch
        {
            GameObject gameObject => gameObject,
            Component component when component != null => component.gameObject,
            _ => Selection.activeGameObject
        };

        /// <summary>
        ///   <para>创建脚本文件；通过包管理获取模版文件所在位置。</para>
        /// </summary>
        /// <param name="t">脚本类型。</param>
        /// <param name="templateFileName">模板文件名称。</param>
        public static void CreateNewScriptFromTemplate(Type t, string templateFileName)
        {
            string[] guids = AssetDatabase.FindAssets($"{templateFileName} t:TextAsset", new [] { UnityEditor.PackageManager.PackageInfo.FindForAssembly(t.Assembly).assetPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, $"New{templateFileName}");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"{templateFileName} Template file not found", "OK");
            }
        }

        /// <summary>
        ///   <para>查找类型的 <see cref="UnityEditor.MonoScript"/>。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="pathFilter">资源路径筛选；null 表示不筛选。</param>
        /// <returns>类型匹配的脚本；未找到时返回 null。</returns>
        public static MonoScript FindMonoScriptForType(Type type, Predicate<string> pathFilter = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (pathFilter != null && !pathFilter(path)) continue;
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type) return script;
            }
            return null;
        }
    }
}

#endif
