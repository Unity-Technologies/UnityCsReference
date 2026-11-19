// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.Search
{
    static partial class SearchProviderUtils
    {
        internal const HideFlags assetSkippedComponentFlags = HideFlags.DontSave | HideFlags.HideInInspector;
        internal const HideFlags sceneSkippedComponentFlags = HideFlags.HideInInspector;
        internal const HideFlags assetSkippedGameObjectFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        internal const HideFlags sceneSkippedGameObjectFlags = HideFlags.HideInHierarchy;

        internal static bool IsSkippedObject(UnityEngine.Object obj, HideFlags skippedFlags)
        {
            return !obj || (obj.hideFlags & skippedFlags) != 0;
        }

        internal static void EnumerateTypeNames(Type objType, bool isPrefabDocument, Action<string, bool> onEnumeration)
        {
            Utils.EnumerateIndexedTypesAndInterfaces(objType, isPrefabDocument, onEnumeration);

            if (typeof(MonoBehaviour).IsAssignableFrom(objType) || typeof(Behaviour).IsAssignableFrom(objType))
            {
                onEnumeration("script", true);
                onEnumeration("monobehaviour", true);
                onEnumeration("behaviour", true);
            }
        }

        internal static bool IsRootGameObject(GameObject go)
        {
            return go.transform.root == go.transform;
        }

        internal static bool IsLeafGameObject(GameObject go)
        {
            return go.transform.childCount == 0;
        }

        internal static string GetPropertyNamePrefix(UnityEngine.Object obj)
        {
            var objType = obj.GetType();
            var isComponent = typeof(Component).IsAssignableFrom(objType);
            return isComponent ? objType.Name + "." : null;
        }

        internal static bool IsHiddenEngineLinkProperty(string propertyPath)
        {
            switch (propertyPath)
            {
                case "m_GameObject":
                case "m_CorrespondingSourceObject":
                case "m_PrefabInstance":
                case "m_PrefabAsset":
                    return true;
                default:
                    return false;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, bool> s_HasHiddenOrSerializeReferenceFieldsCache = new();

        internal static bool HasHiddenOrSerializeReferenceFields(Type scriptType)
        {
            if (!s_HasHiddenOrSerializeReferenceFieldsCache.TryGetValue(scriptType, out var hasFields))
            {
                hasFields = HasHiddenOrSerializeReferenceFields(scriptType, new HashSet<Type>());
                s_HasHiddenOrSerializeReferenceFieldsCache[scriptType] = hasFields;
            }
            return hasFields;
        }

        static bool HasHiddenOrSerializeReferenceFields(Type type, HashSet<Type> visitedTypes)
        {
            if (!visitedTypes.Add(type))
                return false;

            for (var declaringType = type; declaringType != null && declaringType != typeof(MonoBehaviour) && declaringType != typeof(object); declaringType = declaringType.BaseType)
            {
                foreach (var field in declaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    // Which type a [SerializeReference] field holds is only known at runtime, and it may have [HideInInspector] fields, so assume it does.
                    if (field.IsDefined(typeof(HideInInspector), false) || field.IsDefined(typeof(SerializeReference), false))
                        return true;

                    var nestedValueType = GetNestedSerializedValueType(field.FieldType);
                    if (nestedValueType != null && HasHiddenOrSerializeReferenceFields(nestedValueType, visitedTypes))
                        return true;
                }
            }
            return false;
        }

        static Type GetNestedSerializedValueType(Type fieldType)
        {
            var valueType = fieldType;
            if (fieldType.IsArray)
                valueType = fieldType.GetElementType();
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                valueType = fieldType.GetGenericArguments()[0];

            if (valueType.IsPrimitive || valueType.IsEnum || valueType == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(valueType) || !valueType.IsSerializable)
                return null;
            return valueType;
        }
    }
}
