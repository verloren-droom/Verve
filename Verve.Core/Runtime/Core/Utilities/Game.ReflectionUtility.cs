namespace Verve
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>反射工具。</para>
        /// </summary>
        public static class ReflectionUtility
        {
            /// <summary>
            ///   <para>实例成员筛选；包含公开和非公开成员。</para>
            /// </summary>
            private const BindingFlags k_InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            /// <summary>
            ///   <para>逐级枚举字段；包含基类私有字段和各级同名声明。</para>
            /// </summary>
            /// <param name="type">起始类型。</param>
            /// <param name="bindingFlags">成员筛选；指定 <see cref="BindingFlags.DeclaredOnly"/> 时只查本级。</param>
            /// <returns>从派生类到基类的字段序列。</returns>
            public static IEnumerable<FieldInfo> EnumerateFields(Type type, BindingFlags bindingFlags = k_InstanceMembers)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                for (var current = type; current != null; current = current.BaseType)
                {
                    foreach (var field in current.GetFields(bindingFlags | BindingFlags.DeclaredOnly))
                        yield return field;
                    if ((bindingFlags & BindingFlags.DeclaredOnly) != 0) yield break;
                }
            }

            /// <summary>
            ///   <para>从当前类型逐级查找字段，优先返回派生类声明。</para>
            /// </summary>
            /// <param name="type">起始类型。</param>
            /// <param name="fieldName">字段名称。</param>
            /// <param name="bindingFlags">成员筛选；指定 <see cref="BindingFlags.DeclaredOnly"/> 时只查本级。</param>
            /// <returns>匹配的字段；未找到时返回 null。</returns>
            public static FieldInfo FindField(Type type, string fieldName, BindingFlags bindingFlags = k_InstanceMembers)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                for (var current = type; current != null; current = current.BaseType)
                {
                    var field = current.GetField(fieldName, bindingFlags | BindingFlags.DeclaredOnly);
                    if (field != null || (bindingFlags & BindingFlags.DeclaredOnly) != 0) return field;
                }
                return null;
            }

            /// <summary>
            ///   <para>从当前类型逐级查找属性，优先返回派生类声明。</para>
            /// </summary>
            /// <param name="type">起始类型。</param>
            /// <param name="propertyName">属性名称。</param>
            /// <param name="bindingFlags">成员筛选；指定 <see cref="BindingFlags.DeclaredOnly"/> 时只查本级。</param>
            /// <returns>匹配的属性；未找到时返回 null，歧义由 <see cref="AmbiguousMatchException"/> 报告。</returns>
            public static PropertyInfo FindProperty(Type type, string propertyName, BindingFlags bindingFlags = k_InstanceMembers)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                for (var current = type; current != null; current = current.BaseType)
                {
                    var property = current.GetProperty(propertyName, bindingFlags | BindingFlags.DeclaredOnly);
                    if (property != null || (bindingFlags & BindingFlags.DeclaredOnly) != 0) return property;
                }
                return null;
            }

            /// <summary>
            ///   <para>读取实例字段；包含基类的非公开字段。</para>
            /// </summary>
            /// <param name="target">目标实例。</param>
            /// <param name="fieldName">字段名称。</param>
            /// <typeparam name="T">结果类型；按类型转换规则读取，不转换数据格式。</typeparam>
            public static T GetFieldValue<T>(object target, string fieldName)
            {
                var type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
                var field = FindField(type, fieldName) ?? throw new MissingFieldException(type.FullName, fieldName);
                return (T)field.GetValue(target);
            }

            /// <summary>
            ///   <para>写入实例字段；包含基类的非公开字段。</para>
            /// </summary>
            /// <param name="target">目标实例；值类型应传入需要修改的装箱对象。</param>
            /// <param name="fieldName">字段名称。</param>
            /// <param name="value">字段值。</param>
            public static void SetFieldValue(object target, string fieldName, object value)
            {
                var type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
                var field = FindField(type, fieldName) ?? throw new MissingFieldException(type.FullName, fieldName);
                field.SetValue(target, value);
            }

            /// <summary>
            ///   <para>读取实例属性；包含基类的非公开属性，不处理索引器。</para>
            /// </summary>
            /// <param name="target">目标实例。</param>
            /// <param name="propertyName">属性名称。</param>
            /// <typeparam name="T">结果类型；按类型转换规则读取，不转换数据格式。</typeparam>
            public static T GetPropertyValue<T>(object target, string propertyName)
            {
                var type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
                var property = FindProperty(type, propertyName) ?? throw new MissingMemberException(type.FullName, propertyName);
                return (T)property.GetValue(target);
            }

            /// <summary>
            ///   <para>写入实例属性；包含基类的非公开属性，不处理索引器。</para>
            /// </summary>
            /// <param name="target">目标实例；值类型应传入需要修改的装箱对象。</param>
            /// <param name="propertyName">属性名称。</param>
            /// <param name="value">属性值。</param>
            public static void SetPropertyValue(object target, string propertyName, object value)
            {
                var type = target?.GetType() ?? throw new ArgumentNullException(nameof(target));
                var property = FindProperty(type, propertyName) ?? throw new MissingMemberException(type.FullName, propertyName);
                property.SetValue(target, value);
            }
        }
    }
}