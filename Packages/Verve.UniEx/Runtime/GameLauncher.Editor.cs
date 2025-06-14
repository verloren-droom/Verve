#if UNITY_EDITOR
    
namespace VerveUniEx
{
    using Verve;
    using System;
    using Verve.Unit;
    using System.Linq;
    using UnityEngine;
    
    
    [ExecuteInEditMode]
    /// <summary>
    /// 启动器（编辑器模式）
    /// </summary>
    public partial class GameLauncher
    {
        private void OnGUI()
        {
            float buttonWidth = 80f;
            float buttonHeight = 30f;
        
            float buttonX = Screen.width - buttonWidth - 10;
            float buttonY = 10;
        
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight),  Launcher.IsDebug ? "Debug: On" : "Debug: Off"))
            {
                Launcher.IsDebug = !Launcher.IsDebug;
            }
        }

        // [Button("Add Unit")]
        // private void OnAddUnit()
        // {
        //     var unitTypes = AppDomain.CurrentDomain.GetAssemblies()
        //         .SelectMany(a => a.GetTypes())
        //         .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && typeof(ICustomUnit).IsAssignableFrom(t));
        //
        //     foreach (var type in unitTypes)
        //     {
        //         // TODO: 
        //     }
        // }
    }
}

#endif