namespace Verve
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>UI 实例创建器；缓存无参构造函数并限制创建来源。</para>
    /// </summary>
    internal static class UIInstanceCreator
    {
        /// <summary>
        ///   <para>当前允许构造的 UI 基类。</para>
        /// </summary>
        [ThreadStatic] private static Type s_CreatingBaseType;

        /// <summary>
        ///   <para>类型到构造函数的缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, ConstructorInfo> s_Constructors = new();
        /// <summary>
        ///   <para>锁。</para>
        /// </summary>
        private static readonly object s_Lock = new();

        /// <summary>
        ///   <para>返回当前是否正在创建指定 UI 基类的实例。</para>
        /// </summary>
        /// <param name="baseType">允许的基类。</param>
        internal static bool IsCreating(Type baseType) => ReferenceEquals(s_CreatingBaseType, baseType);

        /// <summary>
        ///   <para>创建并缓存指定基类的具体实例。</para>
        /// </summary>
        /// <param name="type">待创建类型。</param>
        /// <param name="baseType">允许的基类。</param>
        /// <param name="typeName">类型显示名称。</param>
        internal static object Create(Type type, Type baseType, string typeName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!baseType.IsAssignableFrom(type) || type.IsAbstract ||
                type.ContainsGenericParameters)
            {
                throw new InvalidOperationException(
                    $"{typeName} 类型 {type.FullName} 不是可创建的具体类型。");
            }

            ConstructorInfo constructor;
            lock (s_Lock)
            {
                if (!s_Constructors.TryGetValue(type, out constructor))
                {
                    constructor = type.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: Type.EmptyTypes,
                        modifiers: null);
                    if (constructor == null)
                    {
                        throw new InvalidOperationException(
                            $"{typeName} 类型 {type.FullName} 必须提供无参构造函数。");
                    }

                    s_Constructors.Add(type, constructor);
                }
            }

            try
            {
                var previousBaseType = s_CreatingBaseType;
                s_CreatingBaseType = baseType;
                try
                {
                    return constructor.Invoke(null);
                }
                finally
                {
                    s_CreatingBaseType = previousBaseType;
                }
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionUtility.Rethrow(exception.InnerException);
                throw;
            }
        }
    }
}