namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>位置组件</para>
    /// </summary>
    [Serializable, NetworkSyncComponent(NetworkSyncDirection.SendAndReceive, 16)]
    public struct PositionComponent : IComponent
    {
        /// <summary>
        ///   <para>X 坐标</para>
        /// </summary>
        [NetworkSyncField] public float x;
        
        /// <summary>
        ///   <para>Y 坐标</para>
        /// </summary>
        [NetworkSyncField] public float y;
        
        /// <summary>
        ///   <para>Z 坐标</para>
        /// </summary>
        [NetworkSyncField] public float z;
        
#if UNITY_5_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(x, y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityEngine.Vector3 ToVector3()
        {
            return new UnityEngine.Vector3(x, y, z);
        }
        
        public static implicit operator UnityEngine.Vector3(PositionComponent p)
        {
            return p.ToVector3();
        }
#endif
        
        public static PositionComponent operator +(PositionComponent a, PositionComponent b)
        {
            return new PositionComponent { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z };
        }

        public static PositionComponent operator -(PositionComponent a, PositionComponent b)
        {
            return new PositionComponent { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z };
        }

        public static PositionComponent operator *(PositionComponent a, float scalar)
        {
            return new PositionComponent { x = a.x * scalar, y = a.y * scalar, z = a.z * scalar };
        }

        public static PositionComponent operator *(float scalar, PositionComponent a)
        {
            return new PositionComponent { x = a.x * scalar, y = a.y * scalar, z = a.z * scalar };
        }

        public static PositionComponent operator /(PositionComponent a, float scalar)
        {
            return new PositionComponent { x = a.x / scalar, y = a.y / scalar, z = a.z / scalar };
        }

        public static bool operator ==(PositionComponent a, PositionComponent b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(PositionComponent a, PositionComponent b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is PositionComponent other && this == other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => HashCode.Combine(x, y, z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => $"PositionComponent(x: {x}, y: {y}, z: {z})";
    }
    
    /// <summary>
    ///   <para>缩放组件</para>
    /// </summary>
    [Serializable, NetworkSyncComponent(NetworkSyncDirection.SendAndReceive, 16)]
    public struct ScaleComponent : IComponent
    {
        /// <summary>
        ///   <para>X 缩放</para>
        /// </summary>
        [NetworkSyncField] public float x;
        
        /// <summary>
        ///   <para>Y 缩放</para>
        /// </summary>
        [NetworkSyncField] public float y;
        
        /// <summary>
        ///   <para>Z 缩放</para>
        /// </summary>
        [NetworkSyncField] public float z;

#if UNITY_5_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(x, y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityEngine.Vector3 ToVector3()
        {
            return new UnityEngine.Vector3(x, y, z);
        }
        
        public static implicit operator UnityEngine.Vector3(ScaleComponent s)
        {
            return s.ToVector3();
        }
#endif
        public static ScaleComponent operator *(ScaleComponent a, ScaleComponent b)
        {
            return new ScaleComponent { x = a.x * b.x, y = a.y * b.y, z = a.z * b.z };
        }

        public static ScaleComponent operator *(ScaleComponent a, float scalar)
        {
            return new ScaleComponent { x = a.x * scalar, y = a.y * scalar, z = a.z * scalar };
        }

        public static ScaleComponent operator *(float scalar, ScaleComponent a)
        {
            return new ScaleComponent { x = a.x * scalar, y = a.y * scalar, z = a.z * scalar };
        }

        public static ScaleComponent operator /(ScaleComponent a, float scalar)
        {
            return new ScaleComponent { x = a.x / scalar, y = a.y / scalar, z = a.z / scalar };
        }

        public static bool operator ==(ScaleComponent a, ScaleComponent b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(ScaleComponent a, ScaleComponent b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is ScaleComponent other && this == other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => HashCode.Combine(x, y, z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => $"ScaleComponent(x: {x}, y: {y}, z: {z})";
    }
    
    /// <summary>
    ///   <para>旋转组件</para>
    /// </summary>
    [Serializable, NetworkSyncComponent(NetworkSyncDirection.SendAndReceive, 16)]
    public struct RotationComponent : IComponent
    {
        /// <summary>
        ///   <para>X 轴旋转</para>
        /// </summary>
        [NetworkSyncField] public float x;
        
        /// <summary>
        ///   <para>Y 轴旋转</para>
        /// </summary>
        [NetworkSyncField] public float y;
        /// <summary>
        ///   <para>Z 轴旋转</para>
        /// </summary>
        [NetworkSyncField] public float z;
        
        /// <summary>
        ///   <para>W 轴旋转</para>
        /// </summary>
        [NetworkSyncField] public float w;

#if UNITY_5_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnityEngine.Quaternion ToQuaternion()
        {
            return new UnityEngine.Quaternion(x, y, z, w);
        }
        
        public static implicit operator UnityEngine.Quaternion(RotationComponent r)
        {
            return r.ToQuaternion();
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => $"RotationComponent(x: {x}, y: {y}, z: {z}, w: {w})";
    }
}