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
        ///   <para>向量工具类。</para>
        /// </summary>
        public static class VectorUtility
        {
            public static Vector3 Bezier(
                Vector3 start,
                Vector3 startTangent,
                Vector3 endTangent,
                Vector3 end,
                float t)
            {
                var inverse = 1f - t;
                return inverse * inverse * inverse * start
                       + 3f * inverse * inverse * t * startTangent
                       + 3f * inverse * t * t * endTangent
                       + t * t * t * end;
            }
        }
    }
}

#endif