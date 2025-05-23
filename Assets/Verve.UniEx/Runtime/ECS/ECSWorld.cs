#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.ECS
{
    using Verve.ECS;
    using UnityEngine;
    
    
    internal class ECSWorld : ComponentInstanceBase<ECSWorld>
    {
        [SerializeField] private SystemContainer m_SystemContainer;
        
        public SystemContainer Systems => m_SystemContainer;
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        internal void Initialize()
        {
            m_SystemContainer ??= new SystemContainer();
            m_SystemContainer?.ForEach(s => s?.Create());
        }

        private void Update()
        {
            m_SystemContainer?.ForEach(s => s?.Update());
        }

        private void OnDestroy()
        {
            m_SystemContainer?.ForEach(s =>
            {
                s?.Destroy();
                m_SystemContainer.UnRegister(s?.GetType());
            });
        }
        
        // public void AddSystems(params Type[] systemTypes)
        // {
        //     
        // }
        //
        // public void AddSystems<T1>() where T1 : SystemBase => AddSystems(typeof(T1));
        // public void AddSystems<T1, T2>() where T1 : SystemBase where T2 : SystemBase => AddSystems(typeof(T1), typeof(T2));
        // public void AddSystems<T1, T2, T3>() where T1 : SystemBase where T2 : SystemBase where T3 : SystemBase => AddSystems(typeof(T1), typeof(T2), typeof(T3));
        // public void AddSystems<T1, T2, T3, T4>() where T1 : SystemBase where T2 : SystemBase where T3 : SystemBase where T4 : SystemBase => AddSystems(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        // public void AddSystems<T1, T2, T3, T4, T5>() where T1 : SystemBase where T2 : SystemBase where T3 : SystemBase where T4 : SystemBase where T5 : SystemBase => AddSystems(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
    }
}
    
#endif