// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Keep in sync with ManagedVersionType in C++
    enum VersionType
    {
        VersionTypeUnity,
        VersionTypePackage,
    }

    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = true)]
    [NativeHeader("Runtime/Scripting/ScriptingManagedProxySupport.h")]
    class VersionMetaData
    {
        public string Name;
        public string Version;
        public VersionType Type;
    }


    class VersionMetaDataComparer : IEqualityComparer<VersionMetaData>
    {
        public bool Equals(VersionMetaData current, VersionMetaData other)
        {
            if (current == null || other == null)
            {
                return current == other;
            }

            return string.Equals(current.Name, other.Name, StringComparison.Ordinal)
                && string.Equals(current.Version, other.Version, StringComparison.Ordinal)
                && current.Type == other.Type;
        }

        public int GetHashCode(VersionMetaData obj)
        {
            unchecked
            {
                var hashCode = (obj.Name != null ? obj.Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.Version != null ? obj.Version.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.Type.GetHashCode();
                return hashCode;
            }
        }
    }
}
