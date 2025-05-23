namespace VerveEditor.UniEx
{
    
#if UNITY_EDITOR
    using UnityEngine;
    
    
    [ExecuteInEditMode]
    /// <summary>
    /// 启动器，框架入口（编辑器模式）
    /// </summary>
    public sealed partial class Launcher : VerveUniEx.GameLauncher
    {
        
    }
#endif
    
}