namespace Verve
{
    
    using Debugger;
    using Unit;


    public class Launcher : InstanceBase<Launcher>
    {
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

        public bool TryGetUnit<TUnit>(out TUnit module) where TUnit : UnitBase, ICustomUnit =>
            m_UnitRules.TryGetDependency<TUnit>(out module);

        public void AddUnit<TUnit>(params object[] args) where TUnit : UnitBase, ICustomUnit => m_UnitRules.AddDependency<TUnit>(args);
        
        public void AddUnit(string moduleName, params object[] args) => m_UnitRules.AddDependency(moduleName, args);
    }
}