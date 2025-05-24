#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx
{
    using Input;
    using Timer;
    using Verve;
    using Loader;
    using System;
    using Storage;
    using Verve.AI;
    using Debugger;
    using Verve.ECS;
    using Verve.MVC;
    using Verve.File;
    using Verve.Unit;
    using Verve.Event;
    using UnityEngine;
    using Verve.Net;
    using Verve.Serializable;
    using System.Collections;
    using ViewUnit = VerveUniEx.MVC.ViewUnit;
    
    
    /// <summary>
    /// 游戏启动器
    /// </summary>
    [AddComponentMenu("Verve/GameLauncher"), DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public partial class GameLauncher : ComponentInstanceBase<GameLauncher>
    {
        public ICustomUnit this[Type unitType] => Launcher.GetUnit(unitType);
        
        public DebuggerUnit Debugger => Launcher.GetUnit<DebuggerUnit>();
        public SerializableUnit Serializable => Launcher.GetUnit<SerializableUnit>();
        public FileUnit File => Launcher.GetUnit<FileUnit>();
        public ECSUnit ECS => Launcher.GetUnit<ECSUnit>();
        public EventUnit Event => Launcher.GetUnit<EventUnit>();
        public InputUnit Input => Launcher.GetUnit<InputUnit>();
        public LoaderUnit Loader => Launcher.GetUnit<LoaderUnit>();
        public MVCUnit MVC => Launcher.GetUnit<MVCUnit>();
        public NetworkUnit Network => Launcher.GetUnit<NetworkUnit>();
        public AIUnit AI => Launcher.GetUnit<AIUnit>();
        public StorageUnit Storage => Launcher.GetUnit<StorageUnit>();
        public ViewUnit View => Launcher.GetUnit<ViewUnit>();
        public TimerUnit Timer => Launcher.GetUnit<TimerUnit>();

        
        IEnumerator Start()
        {
            yield break;
        }

        private void Awake()
        {
            Launcher.AddUnit<DebuggerUnit>();
            Launcher.AddUnit<SerializableUnit>();
            Launcher.AddUnit<FileUnit>();
            Launcher.AddUnit<ECSUnit>();
            Launcher.AddUnit<EventUnit>();
            Launcher.AddUnit<InputUnit>();
            Launcher.AddUnit<LoaderUnit>();
            Launcher.AddUnit<ViewUnit>();
            Launcher.AddUnit<MVCUnit>();
            Launcher.AddUnit<NetworkUnit>();
            Launcher.AddUnit<AIUnit>();
            Launcher.AddUnit<StorageUnit>();
            Launcher.AddUnit<TimerUnit>();
            
            
            Launcher.Initialize();
        }

        private void Update()
        {
            Launcher.Update(Time.deltaTime, Time.unscaledTime);
        }
        
        /// <summary>
        /// 退出游戏（扩展方法）
        /// </summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_5_3_OR_NEWER
            UnityEngine.Application.Quit();
#endif
        }
    }
}
    
#endif