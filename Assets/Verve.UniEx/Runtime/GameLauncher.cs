#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx
{
    using File;
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
    using Verve.Net;
    using Verve.Unit;
    using Verve.Event;
    using UnityEngine;
    using Verve.Serializable;
    using System.Collections;
    using ViewUnit = MVC.ViewUnit;


    /// <summary>
    /// 游戏启动器
    /// </summary>
    [AddComponentMenu("Verve/GameLauncher"), DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public partial class GameLauncher : ComponentInstanceBase<GameLauncher>
    {
        [Header("View")]
        [SerializeField] private Transform m_Parent;

        
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
        public StorageUnit Storage => Launcher.GetUnit<StorageUnit>();
        public ViewUnit View => Launcher.GetUnit<ViewUnit>();
        public TimerUnit Timer => Launcher.GetUnit<TimerUnit>();
        public AIUnit AI => Launcher.GetUnit<AIUnit>();

        
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
            Launcher.AddUnit<ViewUnit>(m_Parent);
            Launcher.AddUnit<MVCUnit>();
            Launcher.AddUnit<NetworkUnit>();
            Launcher.AddUnit<StorageUnit>();
            Launcher.AddUnit<TimerUnit>();
            Launcher.AddUnit<AIUnit>();
            
            
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