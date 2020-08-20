// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct GameManagerDependencyInfo
    {
        [NativeName("managerObjects")]
        internal ObjectIdentifier[] m_ManagerObjects;
        public ReadOnlyCollection<ObjectIdentifier> managerObjects { get { return Array.AsReadOnly(m_ManagerObjects); } }

        [NativeName("referencedObjects")]
        internal ObjectIdentifier[] m_ReferencedObjects;
        public ReadOnlyCollection<ObjectIdentifier> referencedObjects { get { return Array.AsReadOnly(m_ReferencedObjects); } }

        [NativeName("includedTypes")]
        internal Type[] m_IncludedTypes;
        public ReadOnlyCollection<Type> includedTypes { get { return Array.AsReadOnly(m_IncludedTypes); } }
    }
}
