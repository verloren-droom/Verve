#if UNITY_EDITOR

namespace Verve.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///   <para>必需组件属性绘制器。</para>
    /// </summary>
    [CustomPropertyDrawer(typeof(RequireComponentOnGameObjectAttribute))]
    sealed class RequireComponentOnGameObjectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, propertyHeight), property, label, true);
            var warning = GetWarning(property);
            if (warning == null) return;

            position.y += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
            position.height = GetWarningHeight(warning);
            EditorGUI.HelpBox(position, warning, MessageType.Warning);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true);
            var warning = GetWarning(property);
            return warning == null ? height : height + EditorGUIUtility.standardVerticalSpacing + GetWarningHeight(warning);
        }

        /// <summary>
        ///   <para>获取引用对象的组件警告。</para>
        /// </summary>
        /// <param name="property">引用属性。</param>
        private string GetWarning(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return "此特性仅用于 GameObject 或 Component 引用。";
            if (property.hasMultipleDifferentValues || property.objectReferenceValue == null) return null;

            var target = property.objectReferenceValue switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null
            };
            if (target == null) return "此特性仅用于 GameObject 或 Component 引用。";

            var requiredType = ((RequireComponentOnGameObjectAttribute)attribute).RequiredType;
            return target.GetComponent(requiredType) != null
                ? null : $"GameObject '{target.name}' 缺少必要组件：{requiredType.Name}";
        }

        /// <summary>
        ///   <para>计算警告高度。</para>
        /// </summary>
        /// <param name="warning">警告文本。</param>
        private static float GetWarningHeight(string warning)
            => EditorStyles.helpBox.CalcHeight(new GUIContent(warning), EditorGUIUtility.currentViewWidth - 40f);
    }
}

#endif
