// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // A data component whose type is missing after a domain reload, kept as a missing type backup until the type
    // returns. The fields of ManagedReferenceMissingType without the reference id: a GameObject has at most one
    // component of a type.
    // Must match the declaration in EntityComponentReloadBackup.h.
    [NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/EntityComponentReloadBackup.h")]
    internal readonly struct DataComponentMissingType
    {
        public string assemblyName { get { return m_AssemblyName; } }
        public string className { get { return m_ClassName; } }
        public string namespaceName { get { return m_NamespaceName; } }
        public string serializedData { get { return m_SerializedData; } }

#pragma warning disable CS0649
        [NativeName("assemblyName")]
        readonly string m_AssemblyName;

        [NativeName("className")]
        readonly string m_ClassName;

        [NativeName("namespaceName")]
        readonly string m_NamespaceName;

        [NativeName("serializedData")]
        readonly string m_SerializedData;
#pragma warning restore CS0649
    }

    [NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/EntityComponentReloadBackup.h")]
    internal static class EntityComponentReloadBackup
    {
        [FreeFunction("EntityComponentReloadBackup::HasDataComponentsWithMissingTypes")]
        internal static extern bool HasDataComponentsWithMissingTypes(GameObject gameObject);

        [FreeFunction("EntityComponentReloadBackup::GetDataComponentsWithMissingTypes")]
        internal static extern DataComponentMissingType[] GetDataComponentsWithMissingTypes(GameObject gameObject);
    }
}
