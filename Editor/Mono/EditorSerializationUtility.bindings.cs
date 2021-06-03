// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityObject = UnityEngine.Object;
using RefId = System.Int64;

namespace UnityEditor
{
    //Must match declaration in EditorSerializationUtility.h
    [NativeType(Header = "Editor/Src/Utility/EditorSerializationUtility.h")]
    public readonly struct ManagedReferenceMissingType : IEquatable<ManagedReferenceMissingType>, IComparable<ManagedReferenceMissingType>
    {
        public readonly String assemblyName { get { return m_AssemblyName; } }
        public readonly String className { get { return m_ClassName; } }
        public readonly String namespaceName { get { return m_NamespaceName; } }
        public readonly RefId referenceId { get { return m_ReferenceId; } }
        public readonly String serializedData { get { return m_SerializedData; } }

        public bool Equals(ManagedReferenceMissingType other)
        {
            return referenceId == other.referenceId;
        }

        public int CompareTo(ManagedReferenceMissingType other)
        {
            return referenceId.CompareTo(other.referenceId);
        }

#pragma warning disable CS0649
        [NativeName("assemblyName")]
        private readonly String m_AssemblyName;

        [NativeName("className")]
        private readonly String m_ClassName;

        [NativeName("namespaceName")]
        private readonly String m_NamespaceName;

        [NativeName("referenceId")]
        private readonly RefId m_ReferenceId;

        [NativeName("serializedData")]
        private readonly String m_SerializedData;
#pragma warning restore CS0649
    };

    [NativeHeader("Editor/Src/Utility/EditorSerializationUtility.h")]
    public sealed class SerializationUtility
    {
        // Must match the same declarations in "Runtime/Serialize/ReferenceId.h"
        public const RefId RefIdUnknown = -1;
        public const RefId RefIdNull = -2;

        [NativeMethod("SetManagedReferenceIdForObject")]
        static extern bool SetManagedReferenceIdForObjectInternal(UnityObject obj, object scriptObj, RefId refId);

        public static bool SetManagedReferenceIdForObject(UnityObject obj, object scriptObj, RefId refId)
        {
            if (scriptObj == null)
                return refId == RefIdNull; // There is no need to explicitly register RefIdNull

            var valueType = scriptObj.GetType();
            if (valueType == typeof(UnityObject) || valueType.IsSubclassOf(typeof(UnityObject)))
            {
                throw new System.InvalidOperationException(
                    $"Cannot assign an object deriving from UnityEngine.Object to a managed reference. This is not supported.");
            }

            return SetManagedReferenceIdForObjectInternal(obj, scriptObj, refId);
        }

        [NativeMethod("GetManagedReferenceIdForObject")]
        static extern RefId GetManagedReferenceIdForObjectInternal(UnityObject obj, object scriptObj);

        public static  RefId GetManagedReferenceIdForObject(UnityObject obj, object scriptObj)
        {
            return GetManagedReferenceIdForObjectInternal(obj, scriptObj);
        }

        [NativeMethod("GetManagedReference")]
        static extern object GetManagedReferenceInternal(UnityObject obj, RefId id);

        public static object GetManagedReference(UnityObject obj, RefId id)
        {
            return GetManagedReferenceInternal(obj, id);
        }

        [NativeMethod("GetManagedReferenceIds")]
        static extern RefId[] GetManagedReferenceIdsForObjectInternal(UnityObject obj);

        public static RefId[] GetManagedReferenceIds(UnityObject obj)
        {
            return GetManagedReferenceIdsForObjectInternal(obj);
        }

        [NativeMethod("HasManagedReferencesWithMissingTypes")]
        static extern bool HasManagedReferencesWithMissingTypesInternal(UnityObject obj);

        public static bool HasManagedReferencesWithMissingTypes(UnityObject obj)
        {
            return HasManagedReferencesWithMissingTypesInternal(obj);
        }

        [NativeMethod("GetManagedReferencesWithMissingTypes")]
        static extern ManagedReferenceMissingType[] GetManagedReferencesWithMissingTypesInternal(UnityObject obj);

        public static ManagedReferenceMissingType[] GetManagedReferencesWithMissingTypes(UnityObject obj)
        {
            return GetManagedReferencesWithMissingTypesInternal(obj);
        }

        [NativeMethod("ClearAllManagedReferencesWithMissingTypes")]
        static extern bool ClearAllManagedReferencesWithMissingTypesInternal(UnityObject obj);

        public static bool ClearAllManagedReferencesWithMissingTypes(UnityObject obj)
        {
            return ClearAllManagedReferencesWithMissingTypesInternal(obj);
        }

        [NativeMethod("ClearManagedReferenceWithMissingType")]
        static extern bool ClearManagedReferenceWithMissingTypeInternal(UnityObject obj, RefId id);

        public static bool ClearManagedReferenceWithMissingType(UnityObject obj, RefId id)
        {
            return ClearManagedReferenceWithMissingTypeInternal(obj, id);
        }
    };
}
