#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    
    /// <summary>
    ///   <para>模块清单对象引用恢复工具。</para>
    /// </summary>
    internal static class GameModuleManifestObjectRefUtility
    {
        /// <summary>
        ///   <para>序列化属性路径片段类型。</para>
        /// </summary>
        internal enum SerializedPropertyPathElementKind : byte
        {
            /// <summary>
            ///   <para>字段。</para>
            /// </summary>
            Field = 0,
            /// <summary>
            ///   <para>索引。</para>
            /// </summary>
            Index = 1
        }

        /// <summary>
        ///   <para>序列化属性路径片段。</para>
        /// </summary>
        internal readonly struct SerializedPropertyPathElement
        {
            /// <summary>
            ///   <para>种类。</para>
            /// </summary>
            public readonly SerializedPropertyPathElementKind kind;
            /// <summary>
            ///   <para>字段名称。</para>
            /// </summary>
            public readonly string fieldName;
            /// <summary>
            ///   <para>索引。</para>
            /// </summary>
            public readonly int index;

            /// <summary>
            ///   <para>创建序列化属性路径片段。</para>
            /// </summary>
            /// <param name="fieldName">字段名称。</param>
            public SerializedPropertyPathElement(string fieldName)
            {
                kind = SerializedPropertyPathElementKind.Field;
                this.fieldName = fieldName;
                index = -1;
            }

            /// <summary>
            ///   <para>创建序列化属性路径片段。</para>
            /// </summary>
            /// <param name="index">索引。</param>
            public SerializedPropertyPathElement(int index)
            {
                kind = SerializedPropertyPathElementKind.Index;
                fieldName = null;
                this.index = index;
            }
        }

        /// <summary>
        ///   <para>规范化对象引用。</para>
        /// </summary>
        /// <param name="objectRefs">对象引用。</param>
        internal static GameModuleManifestAsset.ImportedObjectRef[] NormalizeObjectRefs(
            GameModuleManifestAsset.ImportedObjectRef[] objectRefs)
        {
            if (objectRefs == null || objectRefs.Length == 0)
            {
                return Array.Empty<GameModuleManifestAsset.ImportedObjectRef>();
            }

            var normalized = new GameModuleManifestAsset.ImportedObjectRef[objectRefs.Length];
            for (int i = 0; i < objectRefs.Length; i++)
            {
                normalized[i] = new GameModuleManifestAsset.ImportedObjectRef
                {
                    path = string.IsNullOrWhiteSpace(objectRefs[i].path)
                        ? string.Empty
                        : objectRefs[i].path.Trim(),
                    asset = objectRefs[i].asset
                };
            }

            return normalized;
        }

        /// <summary>
        ///   <para>尝试赋值对象引用。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="asset">资源。</param>
        /// <param name="error">错误。</param>
        internal static bool TryAssignObjectRef(
            GameModule module,
            string propertyPath,
            UnityEngine.Object asset,
            out string error)
        {
            error = null;
            if (module == null)
            {
                error = "Module is null.";
                return false;
            }

            var pathElements = ParseSerializedPropertyPath(propertyPath, out error);
            if (pathElements == null || pathElements.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(error))
                {
                    error = $"Property path is invalid: {propertyPath ?? "<null>"}";
                }

                return false;
            }

            object boxedModule = module;
            return TryAssignObjectRefRecursive(
                ref boxedModule,
                module.GetType(),
                pathElements,
                0,
                asset,
                propertyPath,
                out error);
        }

        /// <summary>
        ///   <para>尝试赋值对象引用。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        /// <param name="objectRefs">对象引用。</param>
        /// <param name="error">错误。</param>
        internal static bool TryAssignObjectRefs(
            GameModule module,
            GameModuleManifestAsset.ImportedObjectRef[] objectRefs,
            out string error)
        {
            error = null;
            if (module == null)
            {
                error = "Module is null.";
                return false;
            }

            if (objectRefs == null || objectRefs.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < objectRefs.Length; i++)
            {
                var objectRef = objectRefs[i];
                if (string.IsNullOrWhiteSpace(objectRef.path))
                {
                    error = $"Object reference path cannot be empty. referenceIndex={i}";
                    return false;
                }

                if (!TryAssignObjectRef(
                        module,
                        objectRef.path,
                        objectRef.asset,
                        out error))
                {
                    error = $"Failed to restore object reference '{objectRef.path}'. {error}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>解析序列化属性路径。</para>
        /// </summary>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="error">错误。</param>
        internal static List<SerializedPropertyPathElement> ParseSerializedPropertyPath(
            string propertyPath,
            out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                error = "Property path cannot be empty.";
                return null;
            }

            var parts = propertyPath.Split('.');
            var pathElements = new List<SerializedPropertyPathElement>(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrWhiteSpace(part))
                {
                    error = $"Property path contains an empty segment. path={propertyPath}";
                    return null;
                }

                if (string.Equals(part, "Array", StringComparison.Ordinal))
                {
                    continue;
                }

                if (part.StartsWith("data[", StringComparison.Ordinal) &&
                    part.EndsWith("]", StringComparison.Ordinal))
                {
                    if (!int.TryParse(part.Substring(5, part.Length - 6), out var index) || index < 0)
                    {
                        error = $"Property path contains an invalid array index segment. path={propertyPath}, segment={part}";
                        return null;
                    }

                    pathElements.Add(new SerializedPropertyPathElement(index));
                    continue;
                }

                pathElements.Add(new SerializedPropertyPathElement(part));
            }

            return pathElements;
        }

        /// <summary>
        ///   <para>递归恢复对象引用。</para>
        /// </summary>
        /// <param name="current">当前。</param>
        /// <param name="currentType">当前类型。</param>
        /// <param name="pathElements">路径元素。</param>
        /// <param name="elementIndex">元素索引。</param>
        /// <param name="asset">资源。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="error">错误。</param>
        private static bool TryAssignObjectRefRecursive(
            ref object current,
            Type currentType,
            List<SerializedPropertyPathElement> pathElements,
            int elementIndex,
            UnityEngine.Object asset,
            string propertyPath,
            out string error)
        {
            error = null;
            if (currentType == null)
            {
                error = $"Property path '{propertyPath}' reached a null type.";
                return false;
            }

            if (elementIndex < 0 || elementIndex >= pathElements.Count)
            {
                error = $"Property path '{propertyPath}' ended unexpectedly.";
                return false;
            }

            var element = pathElements[elementIndex];
            if (element.kind == SerializedPropertyPathElementKind.Field)
            {
                return TryAssignFieldObjectRef(
                    ref current,
                    currentType,
                    pathElements,
                    elementIndex,
                    asset,
                    propertyPath,
                    out error);
            }

            return TryAssignIndexedObjectRef(
                current,
                currentType,
                pathElements,
                elementIndex,
                asset,
                propertyPath,
                out error);
        }

        /// <summary>
        ///   <para>尝试赋值字段对象引用。</para>
        /// </summary>
        /// <param name="current">当前。</param>
        /// <param name="currentType">当前类型。</param>
        /// <param name="pathElements">路径元素。</param>
        /// <param name="elementIndex">元素索引。</param>
        /// <param name="asset">资源。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="error">错误。</param>
        private static bool TryAssignFieldObjectRef(
            ref object current,
            Type currentType,
            List<SerializedPropertyPathElement> pathElements,
            int elementIndex,
            UnityEngine.Object asset,
            string propertyPath,
            out string error)
        {
            error = null;
            var element = pathElements[elementIndex];
            var field = Game.ReflectionUtility.FindField(currentType, element.fieldName);
            if (field == null)
            {
                error = $"Field '{element.fieldName}' was not found on type {currentType.FullName}.";
                return false;
            }

            if (elementIndex == pathElements.Count - 1)
            {
                if (asset != null && !field.FieldType.IsInstanceOfType(asset))
                {
                    error =
                        $"Field '{field.Name}' on type {currentType.FullName} cannot accept object of type " +
                        $"{asset.GetType().FullName}.";
                    return false;
                }

                field.SetValue(current, asset);
                return true;
            }

            var child = field.GetValue(current);
            if (child == null)
            {
                error =
                    $"Property path '{propertyPath}' cannot traverse null field '{field.Name}' on type {currentType.FullName}.";
                return false;
            }

            object boxedChild = child;
            if (!TryAssignObjectRefRecursive(
                    ref boxedChild,
                    field.FieldType,
                    pathElements,
                    elementIndex + 1,
                    asset,
                    propertyPath,
                    out error))
            {
                return false;
            }

            if (field.FieldType.IsValueType)
            {
                field.SetValue(current, boxedChild);
            }

            return true;
        }

        /// <summary>
        ///   <para>恢复指定索引处的对象引用。</para>
        /// </summary>
        /// <param name="collection">集合。</param>
        /// <param name="collectionType">集合类型。</param>
        /// <param name="pathElements">路径元素。</param>
        /// <param name="elementIndex">元素索引。</param>
        /// <param name="asset">资源。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="error">错误。</param>
        private static bool TryAssignIndexedObjectRef(
            object collection,
            Type collectionType,
            List<SerializedPropertyPathElement> pathElements,
            int elementIndex,
            UnityEngine.Object asset,
            string propertyPath,
            out string error)
        {
            error = null;
            var pathElement = pathElements[elementIndex];
            if (!TryGetIndexedValue(
                    collection,
                    collectionType,
                    pathElement.index,
                    propertyPath,
                    out var elementType,
                    out var element,
                    out error))
            {
                return false;
            }

            if (elementIndex == pathElements.Count - 1)
            {
                if (asset != null && elementType != null && !elementType.IsInstanceOfType(asset))
                {
                    error =
                        $"Collection element at '{propertyPath}' cannot accept object of type {asset.GetType().FullName}.";
                    return false;
                }

                return TrySetIndexedValue(collection, pathElement.index, asset, propertyPath, out error);
            }

            if (element == null)
            {
                error = $"Property path '{propertyPath}' cannot traverse a null collection element.";
                return false;
            }

            object boxedElement = element;
            if (!TryAssignObjectRefRecursive(
                    ref boxedElement,
                    elementType,
                    pathElements,
                    elementIndex + 1,
                    asset,
                    propertyPath,
                    out error))
            {
                return false;
            }

            return elementType == null ||
                   !elementType.IsValueType ||
                   TrySetIndexedValue(collection, pathElement.index, boxedElement, propertyPath, out error);
        }

        /// <summary>
        ///   <para>尝试读取指定索引的值。</para>
        /// </summary>
        /// <param name="collection">集合。</param>
        /// <param name="collectionType">集合类型。</param>
        /// <param name="index">索引。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="elementType">元素类型。</param>
        /// <param name="element">对象实例。</param>
        /// <param name="error">错误。</param>
        private static bool TryGetIndexedValue(
            object collection,
            Type collectionType,
            int index,
            string propertyPath,
            out Type elementType,
            out object element,
            out string error)
        {
            elementType = null;
            element = null;
            error = null;

            if (collection is Array array)
            {
                if (index < 0 || index >= array.Length)
                {
                    error = $"Array index out of range while resolving '{propertyPath}'. index={index}";
                    return false;
                }

                elementType = collectionType?.GetElementType() ?? typeof(object);
                element = array.GetValue(index);
                return true;
            }

            if (collection is IList list)
            {
                if (index < 0 || index >= list.Count)
                {
                    error = $"List index out of range while resolving '{propertyPath}'. index={index}";
                    return false;
                }

                elementType = GetListElementType(collectionType);
                element = list[index];
                return true;
            }

            error = $"Property path '{propertyPath}' expected an array or list but found {GameModuleUtility.GetTypeDisplayName(collectionType)}.";
            return false;
        }

        /// <summary>
        ///   <para>尝试设置指定索引的值。</para>
        /// </summary>
        /// <param name="collection">集合。</param>
        /// <param name="index">索引。</param>
        /// <param name="value">值。</param>
        /// <param name="propertyPath">属性路径。</param>
        /// <param name="error">错误。</param>
        private static bool TrySetIndexedValue(
            object collection,
            int index,
            object value,
            string propertyPath,
            out string error)
        {
            error = null;
            if (collection is Array array)
            {
                if (index < 0 || index >= array.Length)
                {
                    error = $"Array index out of range while assigning '{propertyPath}'. index={index}";
                    return false;
                }

                array.SetValue(value, index);
                return true;
            }

            if (collection is IList list)
            {
                if (index < 0 || index >= list.Count)
                {
                    error = $"List index out of range while assigning '{propertyPath}'. index={index}";
                    return false;
                }

                list[index] = value;
                return true;
            }

            error = $"Property path '{propertyPath}' expected an array or list while assigning object reference.";
            return false;
        }

        /// <summary>
        ///   <para>获取列表元素类型。</para>
        /// </summary>
        /// <param name="collectionType">集合类型。</param>
        private static Type GetListElementType(Type collectionType)
        {
            if (collectionType == null)
            {
                return typeof(object);
            }

            if (collectionType.IsArray)
            {
                return collectionType.GetElementType() ?? typeof(object);
            }

            var listType = FindGenericListType(collectionType);
            return listType?.GetGenericArguments()[0] ?? typeof(object);
        }

        /// <summary>
        ///   <para>查找泛型列表类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static Type FindGenericListType(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return current;
                }
            }

            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var interfaceType = interfaces[i];
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return interfaceType;
                }
            }

            return null;
        }

    }
}

#endif