#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using Verve;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor.SceneManagement;
    using Object = UnityEngine.Object;
    
    
    /// <summary>
    ///   <para>编辑器工具类</para>
    /// </summary>
    public static class CoreEditorUtility
    {
        /// <summary>
        ///   <para>版本Badge样式</para>
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
                    
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.0f));
                tex.Apply();
                style.normal.background = tex;
                    
                return style;
            }
        }

        /// <summary>
        ///   <para>绘制分割线</para>
        /// </summary>
        /// <param name="isBoxed">是否绘制边框</param>
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
        ///   <para>绘制带开关的头部</para>
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="property">属性</param>
        /// <param name="activeProperty">激活属性</param>
        /// <param name="contextClickCallback">右键点击回调</param>
        /// <param name="version">版本</param>
        public static bool DrawHeaderToggle(
            GUIContent title, 
            SerializedProperty property, 
            SerializedProperty activeProperty = null,
            GenericMenu.MenuFunction2 contextClickCallback = null,
            string version = null
            )
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;
            
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            using (new EditorGUI.DisabledScope(activeProperty?.boolValue == false))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            property.serializedObject.Update();
            property.isExpanded = GUI.Toggle(foldoutRect, property.isExpanded, GUIContent.none, EditorStyles.foldout);
            property.serializedObject.ApplyModifiedProperties();

            if (activeProperty != null)
            {
                activeProperty.serializedObject.Update();
                activeProperty.boolValue = GUI.Toggle(toggleRect, activeProperty.boolValue, GUIContent.none, EditorStyles.toggle);
                activeProperty.serializedObject.ApplyModifiedProperties();
            }
            
            if (!string.IsNullOrEmpty(version))
            {
                GUIContent versionContent = new GUIContent($"v{version}");
                Vector2 versionSize = VersionBadgeStyle.CalcSize(versionContent);
                
                Rect versionRect = new Rect(
                    labelRect.x + EditorStyles.boldLabel.CalcSize(title).x + 5f,
                    labelRect.y + (labelRect.height - versionSize.y) / 2f,
                    versionSize.x,
                    versionSize.y
                );
                
                GUI.Label(versionRect, versionContent, VersionBadgeStyle);
            }

            var menuRect = new Rect(labelRect.xMax, labelRect.y, 16, labelRect.height);
            if (GUI.Button(menuRect, EditorGUIUtility.IconContent("_Menu"), new GUIStyle("IconButton")))
            {
                if (contextClickCallback != null)
                {
                    contextClickCallback(menuRect.position);
                }
            }

            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                        property.isExpanded = !property.isExpanded;

                    e.Use();
                }
            }

            return property.isExpanded;
        }
        
        /// <summary>
        ///   <para>创建上下文菜单</para>
        /// </summary>
        /// <param name="menuItems">菜单项</param>
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
        ///   <para>创建上下文菜单</para>
        /// </summary>
        /// <param name="menuItems">菜单项</param>
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
        ///   <para>标记对象为脏并保存</para>
        /// </summary>
        /// <param name="obj">对象</param>
        public static void MarkDirtyAndSave(Object obj)
        {
            EditorUtility.SetDirty(obj);

            if (EditorUtility.IsPersistent(obj))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        ///   <para>获取游戏功能菜单路径</para>
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>
        ///   <para>游戏功能菜单路径</para>
        /// </returns>
        public static string GetGameFeatureMenuPath(Type type)
        {
            var attr = type.GetCustomAttribute<GameFeatureAttribute>();
            if (attr == null)
                return null;
            if (string.IsNullOrEmpty(attr.MenuPath))
                return ObjectNames.NicifyVariableName(type.Name);
            var path = attr.MenuPath.Trim();
            if (path.EndsWith("/"))
                return path + ObjectNames.NicifyVariableName(type.Name);
            return path;
        }
        
        private static GUIStyle m_MiniLabelButton;

        /// <summary>
        ///   <para>小标签按钮样式</para>
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
        ///   <para>获取指定模块中的所有子模块类型</para>
        /// </summary>
        /// <param name="module">模块</param>
        /// <returns>
        ///   <para>子模块类型</para>
        /// </returns>
        public static Type[] GetSubmoduleTypes(GameFeatureModule module)
        {
            var allSubmoduleTypes = TypeCache.GetTypesDerivedFrom<IGameFeatureSubmodule>()
                .Where(t => !t.IsAbstract 
                            && !t.IsInterface 
                            && t.IsClass
                            && t.GetCustomAttribute<GameFeatureSubmoduleAttribute>() != null);
            
            return allSubmoduleTypes
                .Where(t =>
                {
                    var attr = t.GetCustomAttribute<GameFeatureSubmoduleAttribute>();

                    if (attr.BelongsToModule != null && module != null)
                    {
                        return attr.BelongsToModule == module.GetType();
                    }
            
                    if (!string.IsNullOrEmpty(attr.MenuPath) && module != null)
                    {
                        string moduleName = module.name;
                        if (moduleName.Contains("/"))
                        {
                            moduleName = moduleName.Substring(moduleName.LastIndexOf('/') + 1);
                        }
                
                        string submoduleMenuPath = attr.MenuPath;
                        if (submoduleMenuPath.Contains("/"))
                        {
                            submoduleMenuPath = submoduleMenuPath.Substring(submoduleMenuPath.LastIndexOf('/') + 1);
                        }
                
                        return moduleName.Equals(submoduleMenuPath, StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                }).ToArray();
        }
        
        /// <summary>
        ///   <para>获取所有可添加的模块类型（包括普通模块和子模块组合）</para>
        /// </summary>
        public static Dictionary<string, List<Type>> GetAvailableModuleTypes(GameFeatureModuleProfile profile = null)
        {
            var result = new Dictionary<string, List<Type>>();

            var moduleTypes = TypeCache.GetTypesDerivedFrom<GameFeatureModule>()
                .Where(t => !t.IsAbstract && t.IsClass);

            foreach (var type in moduleTypes)
            {
                var menuPath = GetGameFeatureMenuPath(type);
                if (string.IsNullOrEmpty(menuPath)) continue;

                if (!result.ContainsKey(menuPath))
                {
                    result[menuPath] = new List<Type>();
                }
                
                result[menuPath].Add(type);
            }

            var allSubmoduleTypes = TypeCache.GetTypesDerivedFrom<IGameFeatureSubmodule>()
                .Where(t => !t.IsAbstract && t.IsClass && 
                           t.GetCustomAttribute<GameFeatureSubmoduleAttribute>() != null &&
                           t.GetCustomAttribute<GameFeatureSubmoduleAttribute>().BelongsToModule == null);

            var submoduleGroups = allSubmoduleTypes
                .GroupBy(t => t.GetCustomAttribute<GameFeatureSubmoduleAttribute>().MenuPath)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in submoduleGroups)
            {
                string menuPath = group.Key;
                
                if (profile != null)
                {
                    bool alreadyExists = profile.Modules.Any(m => 
                    {
                        if (m == null) return false;
            
                        var moduleMenuPath = GetGameFeatureMenuPath(m.GetType());
                        if (!string.IsNullOrEmpty(moduleMenuPath) && moduleMenuPath == menuPath)
                            return true;
            
                        if (m.Submodules != null && m.Submodules.Count > 0)
                        {
                            var firstSubmodule = m.Submodules.First();
                            var submoduleAttr = firstSubmodule.GetType().GetCustomAttribute<GameFeatureSubmoduleAttribute>();
                            if (submoduleAttr != null && submoduleAttr.MenuPath == menuPath)
                                return true;
                        }
            
                        return false;
                    });
                    
                    if (alreadyExists) continue;
                }
                
                if (!result.ContainsKey(menuPath))
                {
                    result[menuPath] = new List<Type>();
                }
                
                result[menuPath].AddRange(group.Value);
            }

            return result;
        }
        
        /// <summary>
        ///   <para>获取父级对象</para>
        /// </summary>
        public static GameObject GetParentObject(MenuCommand menuCommand)
        {
            GameObject parent;
            
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                parent = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
            }
            else if (menuCommand.context is GameObject context)
            {
                parent = context;
            }
            else
            {
                parent = Selection.activeGameObject;
            }

            return parent;
        }
        
        /// <summary>
        ///   <para>创建脚本文件</para>
        ///   <para>通过包管理获取模版文件所在位置</para>
        /// </summary>
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
        ///   <para>创建纯色纹理</para>
        /// </summary>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="col">背景色</param>
        /// <returns>
        ///   <para>纯色纹理</para>
        /// /returns>
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        /// <summary>
        ///   <para>查找类型的MonoScript</para>
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>
        ///   <para>Mono脚本</para>
        /// /returns>
        public static MonoScript FindMonoScriptForType(Type type)
        {
            if (type == null) return null;
            
            var typeName = type.Name;
            var fullTypeName = type.FullName;
            
            if (typeName.Contains("`"))
            {
                typeName = typeName[..typeName.IndexOf('`')];
            }

            var guids = AssetDatabase.FindAssets("t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null)
                {
                    var scriptClass = script.GetClass();
                    if (scriptClass == type)
                    {
                        return script;
                    }
                    
                    if (script.name == typeName || script.name == type.Name)
                    {
                        return script;
                    }
                    
                    var scriptText = script.text;
                    if (fullTypeName != null && scriptText.Contains(fullTypeName))
                    {
                        return script;
                    }
                }
            }
            
            return null;
        }
    }
}

#endif