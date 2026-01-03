namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>速度组件</para>
    /// </summary>
    [Serializable]
    public struct VelocityComponent : IComponent
    {
        /// <summary>
        ///   <para>轴向速度</para>
        /// </summary>
        public float x, y, z;
        
        /// <summary>
        ///   <para>最大速度</para>
        /// </summary>
        public float maxSpeed;
        
        /// <summary>
        ///   <para>加速度</para>
        /// </summary>
        public float acceleration;
        
        /// <summary>
        ///   <para>减速度</para>
        /// </summary>
        public float deceleration;
        
        /// <summary>
        ///   <para>摩擦力</para>
        /// </summary>
        public float friction;
        
        /// <summary>
        ///   <para>速度向量</para>
        /// </summary>
        public float Magnitude { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (float) Math.Sqrt(x* x + y* y + z* z); }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => HashCode.Combine(x, y, z, maxSpeed, acceleration, deceleration, friction);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => $"VelocityComponent(x: {x}, y: {y}, z: {z}, maxSpeed: {maxSpeed}, acceleration: {acceleration}, deceleration: {deceleration}, friction: {friction})";
    }
}