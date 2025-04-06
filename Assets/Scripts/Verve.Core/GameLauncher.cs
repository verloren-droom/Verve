namespace Verve
{
    using Unit;
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    
using UnityEditor;

    
    /// <summary>
    /// 游戏启动器，框架入口
    /// </summary>
#if UNITY_5_3_OR_NEWER
    public sealed partial class GameLauncher : MonoInstanceBase<GameLauncher>
#else
    public sealed partial class GameLauncher : InstanceBase<GameLauncher>
#endif
    {
#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif
        private UnitRules m_UnitRules = new UnitRules();
        
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
#else
#endif
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}