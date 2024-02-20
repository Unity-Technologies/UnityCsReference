// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Serialization;
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
        [System.Obsolete("Use Serialization.ManagedReferenceUtility.RefIdUnknown instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.Serialization.ManagedReferenceUtility.RefIdUnknown", false)]
        public const RefId RefIdUnknown = -1;
        [System.Obsolete("Use Serialization.ManagedReferenceUtility.RefIdNull instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.Serialization.ManagedReferenceUtility.RefIdNull", false)]
        public const RefId RefIdNull = -2;

        [System.Obsolete("Use Serialization.ManagedReferenceUtility.SetManagedReferenceIdForObject instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.Serialization.ManagedReferenceUtility.SetManagedReferenceIdForObject(*)", false)]
        public static bool SetManagedReferenceIdForObject(UnityObject obj, object scriptObj, RefId refId)
        {
            return ManagedReferenceUtility.SetManagedReferenceIdForObject(obj, scriptObj, refId);
        }

        [System.Obsolete("Use Serialization.ManagedReferenceUtility::GetManagedReferenceIdForObject instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReferenceIdForObject(*)", false)]
        public static  RefId GetManagedReferenceIdForObject(UnityObject obj, object scriptObj)
        {
            return ManagedReferenceUtility.GetManagedReferenceIdForObject(obj, scriptObj);
        }

        [System.Obsolete("Use Serialization.ManagedReferenceUtility::GetManagedReference instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReference(*)", false)]
        public static object GetManagedReference(UnityObject obj, RefId id)
        {
            return ManagedReferenceUtility.GetManagedReference(obj, id);
        }

        [System.Obsolete("Use Serialization.ManagedReferenceUtility::GetManagedReferencesIds instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReferenceIds(*)", false)]
        public static RefId[] GetManagedReferenceIds(UnityObject obj)
        {
            return ManagedReferenceUtility.GetManagedReferenceIds(obj);
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

        internal static extern void SuppressMissingTypeWarning(string className);
    };
}
