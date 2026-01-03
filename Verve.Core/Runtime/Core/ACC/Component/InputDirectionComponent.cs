namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>输入方向组件</para>
    /// </summary>
    [Serializable]
    public struct InputDirectionComponent : IComponent
    {
        /// <summary>
        ///   <para>水平输入方向</para>
        /// </summary>
        public float horizontal;
        
        /// <summary>
        ///   <para>垂直输入方向</para>
        /// </summary>
        public float vertical;
        
        /// <summary>
        ///   <para>跳跃输入</para>
        /// </summary>
        public float jump;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => HashCode.Combine(horizontal, vertical, jump);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => $"InputDirectionComponent(horizontal: {horizontal}, vertical: {vertical}, jump: {jump})";
    }
}