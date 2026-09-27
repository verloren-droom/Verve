#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>全局资源基类；延迟加载类型对应的唯一资源。</para>
    /// </summary>
    /// <typeparam name="T">具体资源类型；使用自身作为类型参数。</typeparam>
    public abstract class ScriptableObjectInstanceBase<T> : ScriptableObject where T : ScriptableObjectInstanceBase<T>
    {
        /// <summary>
        ///   <para>已加载资源；Unity 销毁资源后，下次访问重新读取。</para>
        /// </summary>
        private static T s_Instance;

        /// <summary>
        ///   <para>资源路径；命名空间转换为目录，类名作为文件名。</para>
        /// </summary>
        public static string ResourcePath { get; } = typeof(T).FullName.Replace('.', '/').Replace('+', '/');

        /// <summary>
        ///   <para>唯一资源文件路径。</para>
        /// </summary>
        public static string AssetPath => "Assets/Resources/" + ResourcePath + ".asset";

        /// <summary>
        ///   <para>全局资源；缺失或配置冲突时抛出异常。</para>
        /// </summary>
        public static T Instance => TryGetInstance(out var instance) ? instance
            : throw new InvalidOperationException($"Create a {typeof(T).FullName} asset at {AssetPath} before accessing Instance.");

#if UNITY_EDITOR
        /// <summary>
        ///   <para>资源变化后使缓存失效。</para>
        /// </summary>
        static ScriptableObjectInstanceBase() => UnityEditor.EditorApplication.projectChanged += () => s_Instance = null;
#endif

        /// <summary>
        ///   <para>取得可选资源；仅资源缺失时返回 false，配置冲突仍抛出异常。</para>
        /// </summary>
        /// <param name="instance">借用的资源；缺失时为空。</param>
        public static bool TryGetInstance(out T instance)
        {
            if (s_Instance == null) s_Instance = LoadAsset();
            instance = s_Instance;
            return instance != null;
        }

        /// <summary>
        ///   <para>读取并校验资源；编辑器设置和构建校验不使用实例缓存。</para>
        /// </summary>
        internal static T LoadAsset()
        {
#if UNITY_EDITOR
            var paths = Array.ConvertAll(UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}"),
                UnityEditor.AssetDatabase.GUIDToAssetPath);
            foreach (var path in paths)
                foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is T && (path != AssetPath || !UnityEditor.AssetDatabase.IsMainAsset(asset)))
                        throw new InvalidOperationException($"{typeof(T).FullName} must exist only as the main asset at {AssetPath}. Conflicting asset: {path}.");
            var loaded = UnityEditor.AssetDatabase.LoadMainAssetAtPath(AssetPath);
#else
            var assets = Resources.LoadAll(ResourcePath);
            if (assets.Length > 1)
                throw new InvalidOperationException($"Multiple resources found at {ResourcePath} for {typeof(T).FullName}.");
            var loaded = assets.Length == 0 ? null : assets[0];
#endif
            if (loaded != null && loaded.GetType() != typeof(T))
                throw new InvalidOperationException($"The asset at {AssetPath} must be exactly {typeof(T).FullName}.");
            return (T)loaded;
        }
    }
}

#endif