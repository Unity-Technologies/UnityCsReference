// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageVersion
    {
        string name { get; }

        string displayName { get; }

        string author { get; }

        string authorLink { get; }

        string releaseNotes { get; }

        string description { get; }

        string category { get; }

        IDictionary<string, string> categoryLinks { get; }

        string packageUniqueId { get; }

        string uniqueId { get; }

        IEnumerable<UIError> errors { get; }

        SemVersion? version { get; }

        DateTime? publishedDate { get; }

        DependencyInfo[] dependencies { get; }

        DependencyInfo[] resolvedDependencies { get; }

        PackageInfo packageInfo { get; }

        bool HasTag(PackageTag tag);

        bool isInstalled { get; }

        // A version is fully fetched when the information isn't derived from another version (therefore may be inaccurate)
        bool isFullyFetched { get; }

        bool isAvailableOnDisk { get; }

        bool isDirectDependency { get; }

        string localPath { get; }

        string versionString { get; }

        string versionId { get; }

        SemVersion? supportedVersion { get; }

        IEnumerable<SemVersion> supportedVersions { get; }

        IEnumerable<PackageSizeInfo> sizes { get; }

        EntitlementsInfo entitlements { get; }
    }
}
