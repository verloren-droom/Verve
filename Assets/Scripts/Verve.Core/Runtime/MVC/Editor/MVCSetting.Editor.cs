namespace Verve.MVC.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class MVCSettingEditor
    {
        static MVCSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_MVC");
        }
    }
#endif
}