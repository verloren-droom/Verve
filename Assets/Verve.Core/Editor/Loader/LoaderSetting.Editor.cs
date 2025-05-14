namespace Verve.Loader.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class LoaderSettingEditor
    {
        static LoaderSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_LOADER");
        }
    }
#endif
}