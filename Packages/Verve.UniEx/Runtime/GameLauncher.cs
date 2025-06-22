#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx
{
    using AI;
    using MVC;
    using File;
    using Input;
    using Timer;
    using Verve;
    using Loader;
    using System;
    using Storage;
    using Gameplay;
    using Debugger;
    using Verve.ECS;
    using Verve.Net;
    using Verve.Unit;
    using Verve.Event;
    using UnityEngine;
    using Verve.Serializable;
    using System.Collections;


    /// <summary>
    /// 游戏启动器
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public partial class GameLauncher : ComponentInstanceBase<GameLauncher>
    {
        [SerializeField] private MVCUnitConfig m_MVCConfig;

        public ICustomUnit this[Type unitType] => Launcher.GetUnit(unitType);
        
        public DebuggerUnit Debugger => Launcher.GetUnit<DebuggerUnit>();
        public SerializableUnit Serializable => Launcher.GetUnit<SerializableUnit>();
        public FileUnit File => Launcher.GetUnit<FileUnit>();
        public ECSUnit ECS => Launcher.GetUnit<ECSUnit>();
        public EventBusUnit EventBus => Launcher.GetUnit<EventBusUnit>();
        public InputUnit Input => Launcher.GetUnit<InputUnit>();
        public LoaderUnit Loader => Launcher.GetUnit<LoaderUnit>();
        public MVCUnit MVC => Launcher.GetUnit<MVCUnit>();
        public NetworkUnit Network => Launcher.GetUnit<NetworkUnit>();
        public StorageUnit Storage => Launcher.GetUnit<StorageUnit>();
        public TimerUnit Timer => Launcher.GetUnit<TimerUnit>();
        public AIUnit AI => Launcher.GetUnit<AIUnit>();
        public GameplayUnit Gameplay => Launcher.GetUnit<GameplayUnit>();
        
        public event Action OnInitialize;
        public event Action OnQuit;

        
        protected virtual IEnumerator Start()
        {
            yield break;
        }

        protected virtual void Awake()
        {
            Launcher.AddUnit<DebuggerUnit>();
            Launcher.AddUnit<SerializableUnit>();
            Launcher.AddUnit<FileUnit>();
            Launcher.AddUnit<ECSUnit>();
            Launcher.AddUnit<EventBusUnit>();
            Launcher.AddUnit<InputUnit>();
            Launcher.AddUnit<LoaderUnit>();
            Launcher.AddUnit<MVCUnit>(m_MVCConfig.ViewRootParent);
            Launcher.AddUnit<NetworkUnit>();
            Launcher.AddUnit<StorageUnit>();
            Launcher.AddUnit<TimerUnit>();
            Launcher.AddUnit<AIUnit>();
            Launcher.AddUnit<GameplayUnit>();

            Launcher.Initialize();
            
            OnInitialize?.Invoke();
        }

        protected virtual void Update()
        {
            Launcher.Update(Time.deltaTime, Time.unscaledTime);
        }
        
        protected virtual void OnDestroy()
        {
            Launcher.DeInitialize();
        }
        
        /// <summary>
        /// 退出游戏（扩展方法）
        /// </summary>
        public virtual void Quit()
        {
            OnQuit?.Invoke();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_5_3_OR_NEWER
            UnityEngine.Application.Quit();
#endif
        }
    }
}
    
#endif