namespace Verve.ECS.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class ECSSettingEditor
    {
        static ECSSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_ECS");
        }
    }
#endif
}