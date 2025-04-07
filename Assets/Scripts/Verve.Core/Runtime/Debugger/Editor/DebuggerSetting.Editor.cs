namespace Verve.Debugger.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class DebuggerSettingEditor
    {
        static DebuggerSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_DEBUG");
        }
    }
#endif
}