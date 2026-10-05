// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>能力表单应用模式；控制是否创建能力实例。</para>
    /// </summary>
    public enum CapabilitySheetApplyMode : byte
    {
        /// <summary>
        ///   <para>应用全部内容。</para>
        /// </summary>
        All = 0,
        /// <summary>
        ///   <para>仅应用组件。</para>
        /// </summary>
        ComponentsOnly = 1,
    }

    /// <summary>
    ///   <para>能力表单；描述一组组件、能力和子表单。</para>
    /// </summary>
    [Serializable]
    public sealed class CapabilitySheet
    {
        /// <summary>
        ///   <para>为指定组件类型缓存泛型调用入口。</para>
        /// </summary>
        private static class CapabilitySheetComponentInvoker<T> where T : struct, IComponent
        {
            /// <summary>
            ///   <para>添加组件。</para>
            /// </summary>
            /// <param name="world">目标世界。</param>
            /// <param name="actor">目标行动者。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Add(World world, Actor actor) => world.AddComponent<T>(actor);
            /// <summary>
            ///   <para>移除组件。</para>
            /// </summary>
            /// <param name="world">目标世界。</param>
            /// <param name="actor">目标行动者。</param>
            public static bool Remove(World world, Actor actor) => world.RemoveComponent<T>(actor);
        }

        /// <summary>
        ///   <para>为指定能力类型缓存泛型调用入口。</para>
        /// </summary>
        private static class CapabilitySheetCapabilityInvoker<T> where T : Capability, new()
        {
            /// <summary>
            ///   <para>添加能力。</para>
            /// </summary>
            /// <param name="world">目标世界。</param>
            /// <param name="actor">目标行动者。</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Capability Add(World world, Actor actor) => world.AddCapability<T>(actor);
        }

        /// <summary>
        ///   <para>组件类型到添加委托的缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, Action<World, Actor>> s_AddComponentActions = new(64);
        /// <summary>
        ///   <para>组件类型到移除委托的缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, Func<World, Actor, bool>> s_RemoveComponentFuncs = new(64);
        /// <summary>
        ///   <para>能力类型到添加委托的缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, Func<World, Actor, Capability>> s_AddCapabilityFuncs = new(64);
        /// <summary>
        ///   <para>保护反射委托缓存。</para>
        /// </summary>
        private static readonly object s_MethodCacheLock = new();
        
        /// <summary>
        ///   <para>当前表单包含的能力类型。</para>
        /// </summary>
        private readonly List<Type> m_CapabilityTypes = new(16);
        /// <summary>
        ///   <para>当前表单包含的组件类型。</para>
        /// </summary>
        private readonly List<Type> m_ComponentTypes = new(16);
        /// <summary>
        ///   <para>当前表单包含的子表单。</para>
        /// </summary>
        private readonly List<CapabilitySheet> m_SubSheets = new(8);

        /// <summary>
        ///   <para>能力类型；按添加顺序应用。</para>
        /// </summary>
        public IReadOnlyList<Type> CapabilityTypes => m_CapabilityTypes;
        /// <summary>
        ///   <para>组件类型；按添加顺序应用。</para>
        /// </summary>
        public IReadOnlyList<Type> ComponentTypes => m_ComponentTypes;
        /// <summary>
        ///   <para>子表单；按添加顺序先于当前表单应用。</para>
        /// </summary>
        public IReadOnlyList<CapabilitySheet> SubSheets => m_SubSheets;

        /// <summary>
        ///   <para>按类型缓存静态泛型调用委托。</para>
        /// </summary>
        /// <typeparam name="TDelegate">委托类型。</typeparam>
        /// <param name="cache">目标缓存。</param>
        /// <param name="invokerDefinition">泛型调用器定义。</param>
        /// <param name="targetType">目标类型。</param>
        /// <param name="methodName">静态方法名称。</param>
        private static TDelegate GetCachedDelegate<TDelegate>(
            Dictionary<Type, TDelegate> cache,
            Type invokerDefinition,
            Type targetType,
            string methodName)
            where TDelegate : Delegate
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            lock (s_MethodCacheLock)
            {
                if (cache.TryGetValue(targetType, out var cached)) return cached;
                var method = invokerDefinition.MakeGenericType(targetType)
                    .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                    ?? throw new MissingMethodException(targetType.FullName, methodName);
                var result = (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), method);
                cache[targetType] = result;
                return result;
            }
        }

        /// <summary>
        ///   <para>获取组件移除入口。</para>
        /// </summary>
        /// <param name="componentType">目标类型。</param>
        internal static Func<World, Actor, bool> GetRemoveComponentFunc(Type componentType)
            => GetCachedDelegate(s_RemoveComponentFuncs, typeof(CapabilitySheetComponentInvoker<>), componentType, "Remove");

        /// <summary>
        ///   <para>获取组件创建入口。</para>
        /// </summary>
        /// <param name="componentType">目标类型。</param>
        private static Action<World, Actor> GetAddComponentAction(Type componentType)
            => GetCachedDelegate(s_AddComponentActions, typeof(CapabilitySheetComponentInvoker<>), componentType, "Add");

        /// <summary>
        ///   <para>获取能力创建入口。</para>
        /// </summary>
        /// <param name="capabilityType">目标类型。</param>
        private static Func<World, Actor, Capability> GetAddCapabilityFunc(Type capabilityType)
            => GetCachedDelegate(s_AddCapabilityFuncs, typeof(CapabilitySheetCapabilityInvoker<>), capabilityType, "Add");

        /// <summary>
        ///   <para>添加能力类型。</para>
        /// </summary>
        /// <typeparam name="T">能力类型。</typeparam>
        public CapabilitySheet AddCapability<T>()
            where T : Capability, new()
        {
            m_CapabilityTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        ///   <para>添加能力类型。</para>
        /// </summary>
        /// <param name="capabilityType">可由公开无参构造函数创建的能力类型。</param>
        public CapabilitySheet AddCapability(Type capabilityType)
        {
            if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
            if (!typeof(Capability).IsAssignableFrom(capabilityType))
                throw new ArgumentException($"Type {capabilityType.Name} must inherit from Capability");
            if (capabilityType.IsAbstract)
                throw new ArgumentException($"Type {capabilityType.Name} cannot be abstract");
            if (capabilityType.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException($"Type {capabilityType.Name} must have a public parameterless constructor");

            m_CapabilityTypes.Add(capabilityType);
            return this;
        }

        /// <summary>
        ///   <para>添加组件类型。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        public CapabilitySheet AddComponent<T>()
            where T : struct, IComponent
        {
            m_ComponentTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        ///   <para>添加组件类型。</para>
        /// </summary>
        /// <param name="componentType">组件结构体类型。</param>
        public CapabilitySheet AddComponent(Type componentType)
        {
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            if (!typeof(IComponent).IsAssignableFrom(componentType))
                throw new ArgumentException($"Type {componentType.Name} must implement IComponent");
            if (!componentType.IsValueType)
                throw new ArgumentException($"Component {componentType.Name} must be a struct");

            m_ComponentTypes.Add(componentType);
            return this;
        }

        /// <summary>
        ///   <para>添加子表单；拒绝会形成循环的引用。</para>
        /// </summary>
        /// <param name="subSheet">子表单。</param>
        public CapabilitySheet AddSubSheet(CapabilitySheet subSheet)
        {
            if (subSheet == null) throw new ArgumentNullException(nameof(subSheet));
            if (ReferenceEquals(this, subSheet) || subSheet.Contains(this, new HashSet<CapabilitySheet>()))
                throw new InvalidOperationException("A capability sheet cannot contain itself, directly or indirectly.");
            m_SubSheets.Add(subSheet);
            return this;
        }

        /// <summary>
        ///   <para>递归判断当前表单是否包含目标表单。</para>
        /// </summary>
        /// <param name="target">待查找的表单。</param>
        /// <param name="visited">已访问的表单集合。</param>
        private bool Contains(CapabilitySheet target, HashSet<CapabilitySheet> visited)
        {
            if (ReferenceEquals(this, target)) return true;
            if (!visited.Add(this)) return false;
            for (int i = 0; i < m_SubSheets.Count; i++)
                if (m_SubSheets[i].Contains(target, visited)) return true;
            return false;
        }

        /// <summary>
        ///   <para>应用表单；顶层实例由 <see cref="SheetManager"/> 接管，子实例由父实例接管。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="world">所属世界。</param>
        /// <param name="mode">应用模式。</param>
        internal SheetInstance ApplyTo(Actor actor, World world, CapabilitySheetApplyMode mode)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (!world.IsActorAlive(actor))
                throw new InvalidOperationException("Cannot apply a sheet to a dead actor.");

            var instance = new SheetInstance(actor, world);
            try
            {
                for (int i = 0; i < m_SubSheets.Count; i++)
                    instance.AddSubInstance(m_SubSheets[i].ApplyTo(actor, world, mode));

                for (int i = 0; i < m_ComponentTypes.Count; i++)
                {
                    var componentType = m_ComponentTypes[i];
                    var wasPresent = world.HasComponent(actor, componentType);
                    try
                    {
                        GetAddComponentAction(componentType)(world, actor);
                    }
                    catch
                    {
                        if (!wasPresent && world.HasComponent(actor, componentType))
                            instance.AddComponent(componentType, true);
                        throw;
                    }
                    instance.AddComponent(componentType, !wasPresent);
                }

                if (mode == CapabilitySheetApplyMode.All)
                    for (int i = 0; i < m_CapabilityTypes.Count; i++)
                        instance.AddCapability(GetAddCapabilityFunc(m_CapabilityTypes[i])(world, actor));

                return instance;
            }
            catch (Exception applyException)
            {
                try
                {
                    instance.RemoveFromActor();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Applying the capability sheet failed and rollback was incomplete.", applyException, rollbackException);
                }
                throw;
            }
        }
    }
}