namespace VerveUniEx
{
    
#if UNITY_5_3_OR_NEWER
    using Verve.Debugger;
    using Verve.Unit;
    using UnityEngine;
    
    
    /// <summary>
    /// 启动器，框架入口
    /// </summary>
    public partial class Launcher : ComponentInstanceBase<Launcher>
    {
#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif
        private UnitRules m_UnitRules = new UnitRules();

        private DebuggerUnit m_DebuggerUnit;
        public DebuggerUnit Debugger
        {
            get
            {
                if (m_DebuggerUnit == null)
                {
                    AddUnit<DebuggerUnit>();
                    TryGetUnit<DebuggerUnit>(out m_DebuggerUnit);
                }

                return m_DebuggerUnit;
            }
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public void Initialize()
        {
            m_UnitRules ??= new UnitRules();
            m_UnitRules.Initialize();
        }

        public void Dispose()
        {
            m_UnitRules.Dispose();
        }

        private void Update()
        {
            m_UnitRules.Update(Time.deltaTime, Time.unscaledTime);
        }

        public bool TryGetUnit<TUnit>(out TUnit module) where TUnit : UnitBase, ICustomUnit =>
            m_UnitRules.TryGetDependency<TUnit>(out module);

        public void AddUnit<TUnit>(params object[] args) where TUnit : UnitBase, ICustomUnit => m_UnitRules.AddDependency<TUnit>(args);
        
        public void AddUnit(string moduleName, params object[] args) => m_UnitRules.AddDependency(moduleName, args);
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void Quit()
        {
#if UNITY_5_3_OR_NEWER
            Application.Quit();
#endif
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
#endif
    
}