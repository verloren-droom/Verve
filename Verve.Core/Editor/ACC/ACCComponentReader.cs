// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    ///   <para>只读组件采样；读取当前世界中的数据副本，不调用组件属性或写回存储。</para>
    /// </summary>
    internal static class ACCComponentReader
    {
        private static readonly Dictionary<Type, Func<World, Actor, object>> s_Readers = new();
        private static readonly Dictionary<Type, FieldInfo[]> s_Fields = new();

        internal static bool Read(World world, Actor actor, Dictionary<Type, object> output, HashSet<Type> observed = null)
        {
            output.Clear();
            if (world == null || world.IsDisposed || !world.IsActorAlive(actor)) return false;
            foreach (var id in world.Actors.GetComponentMask(actor))
            {
                var type = ComponentTypeRegistry.GetType(id);
                if (type == null) continue;
                // 未展开的组件只枚举存在性，不读取或装箱其数据。
                if (observed != null && !observed.Contains(type))
                {
                    output.Add(type, null);
                    continue;
                }
                if (!s_Readers.TryGetValue(type, out var read))
                {
                    var method = typeof(ACCComponentReader).GetMethod(nameof(ReadValue), BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(type);
                    read = (Func<World, Actor, object>)Delegate.CreateDelegate(typeof(Func<World, Actor, object>), method);
                    s_Readers.Add(type, read);
                }
                var value = read(world, actor);
                if (value != null) output.Add(type, value);
            }
            return true;
        }

        private static object ReadValue<T>(World world, Actor actor) where T : struct, IComponent
            => world.TryGetComponent<T>(actor, out var value) ? value : null;

        internal static FieldInfo[] GetFields(Type type)
        {
            if (s_Fields.TryGetValue(type, out var cached)) return cached;
            var fields = new List<FieldInfo>();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (!field.IsDefined(typeof(HideInInspector), true) &&
                    (field.IsPublic || field.IsDefined(typeof(SerializeField), true)))
                    fields.Add(field);
            cached = fields.ToArray();
            s_Fields.Add(type, cached);
            return cached;
        }
    }
}

#endif
