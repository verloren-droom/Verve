namespace Verve.Serializable.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class SerializableSettingEditor
    {
        static SerializableSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_SERIALIZABLE");
        }
    }
#endif
}