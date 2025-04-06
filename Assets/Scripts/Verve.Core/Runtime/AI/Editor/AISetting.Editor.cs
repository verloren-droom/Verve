namespace Verve.AI.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class AISettingEditor
    {
        static AISettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_AI");
        }
    }
#endif
}