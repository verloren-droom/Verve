#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;


    /// <summary>
    ///   <para>模块清单对象引用恢复工具</para>
    /// </summary>
    internal static class GameModuleManifestObjectRefUtility
    {
        internal enum SerializedPropertyPathElementKind : byte
        {
            Field = 0,
            Index = 1
        }

        internal readonly struct SerializedPropertyPathElement
        {
            public readonly SerializedPropertyPathElementKind kind;
            public readonly string fieldName;
            public readonly int index;

            public SerializedPropertyPathElement(string fieldName)
            {
                kind = SerializedPropertyPathElementKind.Field;
                this.fieldName = fieldName;
                index = -1;
            }

            public SerializedPropertyPathElement(int index)
            {
                kind = SerializedPropertyPathElementKind.Index;
                fieldName = null;
                this.index = index;
            }
        }

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
            var field = FindSerializedField(currentType, element.fieldName);
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

        private static FieldInfo FindSerializedField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }
    }
}

#endif