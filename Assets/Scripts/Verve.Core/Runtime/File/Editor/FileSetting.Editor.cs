namespace Verve.File.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class FileSettingEditor
    {
        static FileSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_FILE");
        }
    }
#endif
}