#if UNITY_5_3_OR_NEWER

namespace VerveUniEx
{
    using Verve;
    using UnityEngine;
    
    
    /// <summary>
    /// 游戏功能组件基类
    /// </summary>
    [DisallowMultipleComponent]
    public abstract partial class GameFeatureComponent : MonoBehaviour, IGameFeature
    {
        void IGameFeature.Load() => OnLoad();
        void IGameFeature.Activate() => OnActivate();
        void IGameFeature.Deactivate() => OnDeactivate();
        void IGameFeature.Unload() => OnUnload();

        protected virtual void OnLoad() { }
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnUnload() { }
        
        private void OnDestroy() => ((IGameFeature)this).Unload();
    }
}

#endif