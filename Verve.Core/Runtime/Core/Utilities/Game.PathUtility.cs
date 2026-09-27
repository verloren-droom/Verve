namespace Verve
{
    using System;

    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>路径工具；统一分隔符、Unity 项目路径和本地文件 URL。</para>
        /// </summary>
        public static class PathUtility
        {
            /// <summary>
            ///   <para>项目路径比较器。</para>
            /// </summary>
            public static readonly StringComparer ProjectPathComparer = StringComparer.OrdinalIgnoreCase;

            /// <summary>
            ///   <para>规范化路径分隔符。</para>
            /// </summary>
            /// <param name="path">路径。</param>
            public static string Normalize(string path) => path.Replace('\\', '/').Trim();

            /// <summary>
            ///   <para>规范化项目路径。</para>
            /// </summary>
            /// <param name="path">路径。</param>
            /// <param name="paramName">参数名称。</param>
            public static string NormalizeProjectPath(string path, string paramName = "path")
            {
                if (!TryNormalizeProjectPath(path, out var normalized))
                    throw new ArgumentException("Expected an Assets/ or Packages/ file path without empty, '.' or '..' segments.", paramName);
                return normalized;
            }

            /// <summary>
            ///   <para>尝试规范化项目路径。</para>
            /// </summary>
            /// <param name="path">路径。</param>
            /// <param name="normalized">规范化。</param>
            public static bool TryNormalizeProjectPath(string path, out string normalized)
            {
                normalized = null;
                if (string.IsNullOrWhiteSpace(path)) return false;
                var candidate = Normalize(path);
                string root;
                if (candidate.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) root = "Assets/";
                else if (candidate.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)) root = "Packages/";
                else return false;

                foreach (var segment in candidate.Substring(root.Length).Split('/'))
                {
                    if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == "..") return false;
                }
                normalized = root + candidate.Substring(root.Length);
                return true;
            }

            /// <summary>
            ///   <para>规范化场景路径。</para>
            /// </summary>
            /// <param name="path">路径。</param>
            /// <param name="paramName">参数名称。</param>
            public static string NormalizeScenePath(string path, string paramName = "path")
            {
                var normalized = NormalizeProjectPath(path, paramName);
                if (!normalized.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Expected a .unity scene asset path.", paramName);
                return normalized;
            }

            /// <summary>
            ///   <para>本地绝对路径转换为 file URL；保留 Android jar 和远程 URL。</para>
            /// </summary>
            /// <param name="path">路径。</param>
            public static string ToRequestUrl(string path)
            {
                var normalized = Normalize(path);
                return normalized.Contains("://") ? normalized : new Uri(System.IO.Path.GetFullPath(normalized)).AbsoluteUri;
            }
        }
    }
}