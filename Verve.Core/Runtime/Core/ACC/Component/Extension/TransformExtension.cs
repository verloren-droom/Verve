namespace Verve
{
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>变换组件扩展类</para>
    /// </summary>
    public static class TransformExtension
    {
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>从Transform中拉取本地数据</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PullFromLocalTransform(this Actor self, UnityEngine.Transform transform, World world)
        {
            if (transform == null || world == null || !self.IsAlive(world))
                return;
    
            if (self.HasComponent<PositionComponent>(world))
            {
                ref var pos = ref self.GetComponent<PositionComponent>(world);
                var v = transform.localPosition;
                pos.x = v.x;
                pos.y = v.y;
                pos.z = v.z;
            }
    
            if (self.HasComponent<RotationComponent>(world))
            {
                ref var rot = ref self.GetComponent<RotationComponent>(world);
                var q = transform.localRotation;
                rot.x = q.x;
                rot.y = q.y;
                rot.z = q.z;
                rot.w = q.w;
            }
    
            if (self.HasComponent<ScaleComponent>(world))
            {
                ref var scl = ref self.GetComponent<ScaleComponent>(world);
                var v = transform.localScale;
                scl.x = v.x;
                scl.y = v.y;
                scl.z = v.z;
            }
        }
        
        /// <summary>
        ///   <para>从Transform中拉取本地数据</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PullFromLocalTransform(this Actor self, UnityEngine.Transform transform)
            => PullFromLocalTransform(self, transform, Game.World);
#endif
    
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>从Transform中拉取全局数据</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PullFromTransform(this Actor self, UnityEngine.Transform transform, World world)
        {
            if (transform == null || world == null || !self.IsAlive(world))
                return;
    
            if (self.HasComponent<PositionComponent>(world))
            {
                ref var pos = ref self.GetComponent<PositionComponent>(world);
                var v = transform.position;
                pos.x = v.x;
                pos.y = v.y;
                pos.z = v.z;
            }
    
            if (self.HasComponent<RotationComponent>(world))
            {
                ref var rot = ref self.GetComponent<RotationComponent>(world);
                var q = transform.rotation;
                rot.x = q.x;
                rot.y = q.y;
                rot.z = q.z;
                rot.w = q.w;
            }
    
            if (self.HasComponent<ScaleComponent>(world))
            {
                ref var scl = ref self.GetComponent<ScaleComponent>(world);
                var v = transform.lossyScale;
                scl.x = v.x;
                scl.y = v.y;
                scl.z = v.z;
            }
        }
        
        /// <summary>
        ///   <para>从Transform中拉取全局数据</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PullFromTransform(this Actor self, UnityEngine.Transform transform)
            => PullFromTransform(self, transform, Game.World);
#endif
        
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>将数据推送到Transform的本地坐标</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushToLocalTransform(this Actor self, UnityEngine.Transform transform, World world)
        {
            if (transform == null || world == null || !self.IsAlive(world))
                return;
    
            if (self.HasComponent<PositionComponent>(world))
            {
                ref var pos = ref self.GetComponent<PositionComponent>(world);
                transform.localPosition = pos.ToVector3();
            }
    
            if (self.HasComponent<RotationComponent>(world))
            {
                ref var rot = ref self.GetComponent<RotationComponent>(world);
                transform.localRotation = rot.ToQuaternion();
            }
    
            if (self.HasComponent<ScaleComponent>(world))
            {
                ref var scl = ref self.GetComponent<ScaleComponent>(world);
                transform.localScale = scl.ToVector3();
            }
        }
        
        /// <summary>
        ///   <para>将数据推送到Transform的本地坐标</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushToLocalTransform(this Actor self, UnityEngine.Transform transform)
            => PushToLocalTransform(self, transform, Game.World);
#endif
    
#if UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>将数据推送到Transform的全局坐标</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushToTransform(this Actor self, UnityEngine.Transform transform, World world)
        {
            if (transform == null || world == null || !self.IsAlive(world))
                return;
    
            if (self.HasComponent<PositionComponent>(world))
            {
                ref var pos = ref self.GetComponent<PositionComponent>(world);
                transform.position = pos.ToVector3();
            }
    
            if (self.HasComponent<RotationComponent>(world))
            {
                ref var rot = ref self.GetComponent<RotationComponent>(world);
                transform.rotation = rot.ToQuaternion();
            }
    
            if (self.HasComponent<ScaleComponent>(world))
            {
                ref var scl = ref self.GetComponent<ScaleComponent>(world);
                transform.localScale = scl.ToVector3(); 
            }
        }
        
        /// <summary>
        ///   <para>将数据推送到Transform的全局坐标</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushToTransform(this Actor self, UnityEngine.Transform transform)
            => PushToTransform(self, transform, Game.World);
#endif
    }
}