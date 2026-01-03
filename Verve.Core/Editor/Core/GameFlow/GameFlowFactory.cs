#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    
    
    /// <summary>
    ///   <para>游戏流程工厂</para>
    /// </summary>
    internal static class GameFlowFactory
    {
        /// <summary>
        ///   <para>游戏流程节点模版文件名</para>
        ///   <para>不包含.txt文件后缀</para>
        /// </summary>
        const string NODE_TEMPLATE_FILENAME = "GameFlowNode.cs";

        /// <summary>
        ///   <para>创建游戏流程节点</para>
        /// </summary>
        [MenuItem("Assets/Create/Verve/GameFlow/GameFlowNode")]
        private static void CreateNewGameFeatureComponentScript()
            => CoreEditorUtility.CreateNewScriptFromTemplate(typeof(GameFlowFactory), NODE_TEMPLATE_FILENAME);
    }
}

#endif