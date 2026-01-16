namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>同步变换能力</para>
    ///   <para>依赖<see cref="PositionComponent"/>组件，<see cref="GameObjectRef"/>组件</para>
    /// </summary>
    [Serializable]
    public sealed class SyncTransformCapability : Capability
    {
        protected override void OnSetup()
        {
            Require<PositionComponent>();
#if UNITY_5_3_OR_NEWER
            Require<GameObjectRef>();
#endif
            SetTick(TickGroup.Late, 0);
        }
        
        protected internal override void TickActive(in float deltaTime)
        {
#if UNITY_5_3_OR_NEWER
            if (this.TryGetComponent(out GameObjectRef goRef) && goRef.IsValid)
            {
                OwnerActor.PushToTransform(goRef.go.transform, OwnerWorld);
            }
#endif
        }
    }
}