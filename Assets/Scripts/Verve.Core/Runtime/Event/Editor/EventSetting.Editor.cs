namespace Verve.Event.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class EventSettingEditor
    {
        static EventSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_EVENT");
        }
    }
#endif
}