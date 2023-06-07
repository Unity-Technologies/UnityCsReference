// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    internal static class ClassLibraryInitializer
    {
        [RequiredByNativeCode]
        static void Init()
        {
            UnityLogWriter.Init();
        }

        [RequiredByNativeCode]
        static void InitStdErrWithHandle(IntPtr fileHandle)
        {
            var sfh = new SafeFileHandle(fileHandle, false);
            if (!sfh.IsInvalid)
            {
                var writer = new StreamWriter(new FileStream(sfh, FileAccess.Write)) { AutoFlush = true };
                Console.SetError(writer);
            }
        }

        // If the AssemblyResolve call fails to load the assembly, this could lead
        // to infinite recursion.  Mono projects against this, but CoreCLR does not,
        // So this causes infinite recursion on CoreCLR.
        // This logic should be handled by the AssemblyLoadContext in the CoreCLR host
        [RequiredByNativeCode(Optional = true)]
        static void InitAssemblyRedirections()
        {
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Mono/AssemblyFullName.h")]
    [RequiredByNativeCode(GenerateProxy = true)]
    struct AssemblyVersion
    {
        public ushort major;
        public ushort minor;
        public ushort build;
        public ushort revision;

        public AssemblyVersion(ushort major, ushort minor, ushort build, ushort revision)
        {
            this.major = major;
            this.minor = minor;
            this.build = build;
            this.revision = revision;
        }

        public static bool operator ==(AssemblyVersion lhs, AssemblyVersion rhs)
        {
            return lhs.major == rhs.major
                && lhs.minor == rhs.minor
                && lhs.build == rhs.build
                && lhs.revision == rhs.revision;
        }

        public static bool operator !=(AssemblyVersion lhs, AssemblyVersion rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator <(AssemblyVersion lhs, AssemblyVersion rhs)
        {
            if (lhs.major != rhs.major)
                return lhs.major < rhs.major;
            if (lhs.minor != rhs.minor)
                return lhs.minor < rhs.minor;
            if (lhs.build != rhs.build)
                return lhs.build < rhs.build;
            if (lhs.revision != rhs.revision)
                return lhs.revision < rhs.revision;
            return false;
        }

        public static bool operator >(AssemblyVersion lhs, AssemblyVersion rhs)
        {
            if (lhs.major != rhs.major)
                return lhs.major > rhs.major;
            if (lhs.minor != rhs.minor)
                return lhs.minor > rhs.minor;
            if (lhs.build != rhs.build)
                return lhs.build > rhs.build;
            if (lhs.revision != rhs.revision)
                return lhs.revision > rhs.revision;
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{build}.{revision}";
        }

        public override bool Equals(object other)
        {
            return other is AssemblyVersion otherVersion
                && major == otherVersion.major
                && minor == otherVersion.minor
                && build == otherVersion.build
                && revision == otherVersion.revision;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(major, minor, build, revision);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Mono/AssemblyFullName.h")]
    [RequiredByNativeCode(GenerateProxy = true)]
    struct AssemblyFullName
    {
        [NativeName("name")]
        public string Name;
        [NativeName("version")]
        public AssemblyVersion Version;
        [NativeName("publicKeyToken")]
        public string PublicKeyToken;
        [NativeName("culture")]
        public string Culture;

        public override bool Equals(object other)
        {
            return other is AssemblyFullName otherVersion
                && Name == otherVersion.Name
                && Version == otherVersion.Version
                && PublicKeyToken == otherVersion.PublicKeyToken
                && Culture == otherVersion.Culture;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Version, PublicKeyToken, Culture);
        }

        public override string ToString()
        {
            return $"{Name}, Version={Version}, Culture={(string.IsNullOrEmpty(Culture) ? "neutral" : Culture)}, PublicKeyToken={PublicKeyToken}";
        }
    }
}
