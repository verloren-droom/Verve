#if UNITY_EDITOR
    
namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;
    
    /// <summary>
    ///   <para>只读属性绘制器。</para>
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    sealed class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);
    }
}
    
#endif
