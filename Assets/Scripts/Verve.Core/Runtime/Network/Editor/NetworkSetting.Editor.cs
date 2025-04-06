namespace Verve.Network.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    
    
    [InitializeOnLoad]
    public static partial class NetworkSettingEditor
    {
        static NetworkSettingEditor()
        {
            FrameworkSettingEditor.AddMacro("VERVE_FRAMEWORK_NETWORK");
        }
    }
#endif
}