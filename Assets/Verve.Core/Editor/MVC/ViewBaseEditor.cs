
namespace Verve.MVC
{
#if UNITY_5_3_OR_NEWER && UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    
    // 自定义编辑器脚本，用于在编辑器中处理拖拽逻辑
    [CustomEditor(typeof(ViewBase))]
    public class ViewBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ViewBase viewBase = (ViewBase)target;
        }
    }
#endif
}