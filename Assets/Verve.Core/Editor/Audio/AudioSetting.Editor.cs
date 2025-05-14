namespace Verve.Audio.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class AudioSettingEditor
    {
        static AudioSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_AUDIO");
        }
    }
#endif
}