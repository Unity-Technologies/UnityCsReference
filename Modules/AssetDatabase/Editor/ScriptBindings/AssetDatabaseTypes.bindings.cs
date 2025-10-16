// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable

using System;

using UnityEngine.Scripting;
using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.Experimental
{
        [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseTypes.h")]
        [NativeClass("::AssetDatabase::ImportResultID")]
        [RequiredByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
    public struct ImportResultID : IEquatable<ImportResultID>
    {
        internal ImportResultID(Hash128 value)
        {
            this.value = value;
        }

        public bool Equals(ImportResultID other)
        {
            return value.Equals(other.value);
        }

        public bool Equals(Hash128 other)
        {
            return value.Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        //TODO: this is internal rather than private for backwards compatibility with the entities package.
        //See com.unity.entities/Unity.Scenes/AssetDatabaseCompatibility.cs in 1.5.0-exp.1
        //This should become private in a future release - this type should be opaque because value is an implementation detail.
        internal Hash128 value;

        public bool isValid => !value.Equals(default);
    }
}
