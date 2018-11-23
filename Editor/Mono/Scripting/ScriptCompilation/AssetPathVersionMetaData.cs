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
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode(GenerateProxy = true)]
    [NativeHeader("Runtime/Scripting/ScriptingManagedProxySupport.h")]
    class AssetPathVersionMetaData
    {
        public string Name;
        public string Version;
    }

    class AssetPathVersionMetaDataComparer : IEqualityComparer<AssetPathVersionMetaData>
    {
        public bool Equals(AssetPathVersionMetaData current, AssetPathVersionMetaData other)
        {
            if (current == null || other == null)
            {
                return current == other;
            }

            return string.Equals(current.Name, other.Name) && string.Equals(current.Version, other.Version);
        }

        public int GetHashCode(AssetPathVersionMetaData obj)
        {
            unchecked
            {
                return ((obj.Name != null ? obj.Name.GetHashCode() : 0) * 397) ^ (obj.Version != null ? obj.Version.GetHashCode() : 0);
            }
        }
    }
}
