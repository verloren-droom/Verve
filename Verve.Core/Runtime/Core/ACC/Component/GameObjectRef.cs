#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>游戏对象引用</para>
    /// </summary>
    [Serializable]
    public struct GameObjectRef : IComponent, IEquatable<GameObjectRef>
    {
        /// <summary>
        ///   <para>游戏对象</para>
        /// </summary>
        public GameObject go;
        
        /// <summary>
        ///   <para>对象是否有效</para>
        /// </summary>
        public bool IsValid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => go != null; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => $"Ref: {go?.name ?? "Unknown"}";
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => go == null ? 0 : go.GetHashCode();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object obj) => obj is GameObjectRef other && go == other.go;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(GameObjectRef other) => go == other.go;
        // [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator GameObject(GameObjectRef ref_) => ref_.go;
        // [MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator GameObjectRef(GameObject go) => new GameObjectRef { go = go };
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(GameObjectRef lhs, GameObjectRef rhs) => lhs.go == rhs.go;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(GameObjectRef lhs, GameObjectRef rhs) => lhs.go != rhs.go;
    }
}

#endif