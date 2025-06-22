#if UNITY_EDITOR

namespace VerveUniEx.Gameplay
{
    using UnityEditor;
    using UnityEngine;
    
    
    [AddComponentMenu("Verve/Gameplay/Character/AIController"), DisallowMultipleComponent, ExecuteAlways]
    public partial class AIController
    {
        /// <summary>
        /// 创建AI
        /// </summary>
        [MenuItem("GameObject/Verve/Gameplay/Character/AI", false, 10)]
        private static void CreateAI(MenuCommand menuCommand)
        {
            // var prefab = new GameObject("AI (Clone)");
        }
    }
}

#endif