namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>工具配置；保存能力接口与项目实现的映射。</para>
    /// </summary>
    [Serializable]
    public sealed class GameToolConfiguration
    {
        /// <summary>
        ///   <para>项目实现选择；未出现的接口使用其默认实现。</para>
        /// </summary>
#if UNITY_5_3_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        internal List<Selection> selections = new();

        /// <summary>
        ///   <para>工具选择；仅保存类型名称。</para>
        /// </summary>
        [Serializable]
        internal sealed class Selection
        {
            /// <summary>
            ///   <para>能力接口的程序集限定名。</para>
            /// </summary>
            public string contract;
            /// <summary>
            ///   <para>实现类型的程序集限定名。</para>
            /// </summary>
            public string implementation;
        }

        /// <summary>
        ///   <para>选择项目实现。</para>
        /// </summary>
        /// <typeparam name="TContract">工具能力接口。</typeparam>
        /// <typeparam name="TImplementation">项目实现类型。</typeparam>
        /// <returns>当前配置。</returns>
        public GameToolConfiguration Set<TContract, TImplementation>()
            where TContract : class, IGameTool where TImplementation : class, TContract, new() =>
            Set(typeof(TContract), typeof(TImplementation));

        /// <summary>
        ///   <para>选择项目实现；空实现移除选择并使用接口默认值。</para>
        /// </summary>
        /// <param name="contract">工具能力接口。</param>
        /// <param name="implementation">项目实现类型。</param>
        /// <returns>当前配置。</returns>
        public GameToolConfiguration Set(Type contract, Type implementation)
        {
            Validate(contract, implementation);
            var name = contract.AssemblyQualifiedName;
            selections.RemoveAll(selection => selection.contract == name);
            if (implementation != null)
                selections.Add(new Selection { contract = name, implementation = implementation.AssemblyQualifiedName });
            return this;
        }

        /// <summary>
        ///   <para>读取配置快照；拒绝失效或重复的类型映射。</para>
        /// </summary>
        internal Dictionary<Type, Type> GetSelections()
        {
            var result = new Dictionary<Type, Type>();
            foreach (var selection in selections)
            {
                var contract = Type.GetType(selection.contract, throwOnError: true);
                var implementation = Type.GetType(selection.implementation, throwOnError: true);
                Validate(contract, implementation);
                result.Add(contract, implementation);
            }
            return result;
        }

        /// <summary>
        ///   <para>判断类型是否为可配置的工具接口。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static bool IsContract(Type type) => type != null && type.IsInterface && type.IsVisible &&
            !type.ContainsGenericParameters && type != typeof(IGameTool) && typeof(IGameTool).IsAssignableFrom(type) &&
            !typeof(IDisposable).IsAssignableFrom(type) && !typeof(IAsyncDisposable).IsAssignableFrom(type) &&
            !typeof(IGameModule).IsAssignableFrom(type);

        /// <summary>
        ///   <para>判断实现是否为无生命周期的普通工具。</para>
        /// </summary>
        /// <param name="type">实现类型。</param>
        /// <param name="contract">能力接口。</param>
        /// <param name="allowInternal">是否允许接口声明的内部默认实现。</param>
        internal static bool IsSupported(Type type, Type contract, bool allowInternal = false) =>
            IsContract(contract) && type != null && type.IsClass &&
            (type.IsVisible || allowInternal && type.Assembly == contract.Assembly) &&
            !type.IsAbstract && !type.ContainsGenericParameters && contract.IsAssignableFrom(type) &&
            type.GetConstructor(Type.EmptyTypes) != null &&
#if UNITY_5_3_OR_NEWER
            !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
#endif
            !typeof(IDisposable).IsAssignableFrom(type) && !typeof(IAsyncDisposable).IsAssignableFrom(type) &&
            !typeof(IGameModule).IsAssignableFrom(type);

        /// <summary>
        ///   <para>校验能力接口与实现。</para>
        /// </summary>
        /// <param name="contract">能力接口。</param>
        /// <param name="implementation">实现类型；空值表示默认配置。</param>
        internal static void Validate(Type contract, Type implementation)
        {
            if (!IsContract(contract)) throw new ArgumentException($"'{contract}' must be a public tool interface without a lifecycle.", nameof(contract));
            if (implementation != null && !IsSupported(implementation, contract))
                throw new ArgumentException($"'{implementation}' must be a public, concrete {contract.Name} class with a public parameterless constructor and no lifecycle. Use GameModule for persistent resources.", nameof(implementation));
        }
    }
}