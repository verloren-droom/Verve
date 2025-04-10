namespace Verve.Input.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class InputSettingEditor
    {
        static InputSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_INPUT");
        }
    }
#endif
}