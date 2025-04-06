namespace Verve.Storage.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class StorageSettingEditor
    {
        static StorageSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_STORAGE");
        }
    }
#endif
}