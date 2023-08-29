// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerBaseAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public PackageManagerBaseAnalytic(string eventName) : base(eventName, 1, UnityEngine.Analytics.SendEventOptions.kAppendNone, "packageManager") { }
        public Int64 start_ts;
        public Int64 duration;
        public bool blocking;

        public string package_id;
        public int status_code;
        public string error_message;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerAddPackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerAddPackageAnalytic() : base("addPackage") { }

        [UsedByNativeCode]
        internal static PackageManagerAddPackageAnalytic CreatePackageManagerAddPackageAnalytic() { return new PackageManagerAddPackageAnalytic(); }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerTestAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerTestAnalytic() : base("PackageManager") { }

        [UsedByNativeCode]
        internal static PackageManagerTestAnalytic CreatePackageManagerTestAnalytic() { return new PackageManagerTestAnalytic(); }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerRemovePackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerRemovePackageAnalytic() : base("removePackage") { }

        [UsedByNativeCode]
        internal static PackageManagerRemovePackageAnalytic CreatePackageManagerRemovePackageAnalytic() { return new PackageManagerRemovePackageAnalytic(); }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerResolvePackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerResolvePackageAnalytic() : base("resolvePackages") { }

        [UsedByNativeCode]
        internal static PackageManagerResolvePackageAnalytic CreatePackageManagerResolvePackageAnalytic() { return new PackageManagerResolvePackageAnalytic(); }

        public string[] packages;
        public string[] package_registries;
        public string[] package_signatures;
        public string[] package_sources;
        public string[] package_types;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerEmbedPackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerEmbedPackageAnalytic() : base("embedPackage") { }

        [UsedByNativeCode]
        internal static PackageManagerEmbedPackageAnalytic CreatePackageManagerEmbedPackageAnalytic() { return new PackageManagerEmbedPackageAnalytic(); }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerResetPackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerResetPackageAnalytic() : base("resetToDefaultDependencies") { }

        [UsedByNativeCode]
        internal static PackageManagerResetPackageAnalytic CreatePackageManagerResetPackageAnalytic() { return new PackageManagerResetPackageAnalytic(); }
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerResolveErrorPackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerResolveErrorPackageAnalytic() : base("resolveErrorUserAction") { }

        [UsedByNativeCode]
        internal static PackageManagerResolveErrorPackageAnalytic CreatePackageManagerResolveErrorPackageAnalytic() { return new PackageManagerResolveErrorPackageAnalytic(); }

        public string reason;
        public string action;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class PackageManagerStartServerPackageAnalytic : PackageManagerBaseAnalytic
    {
        public PackageManagerStartServerPackageAnalytic() : base("startPackageManagerServer") { }

        [UsedByNativeCode]
        internal static PackageManagerStartServerPackageAnalytic CreatePackageManagerStartServerPackageAnalytic() { return new PackageManagerStartServerPackageAnalytic(); }
    }
}
