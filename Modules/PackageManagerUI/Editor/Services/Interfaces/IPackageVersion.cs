// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageVersion
    {
        string name { get; }

        string displayName { get; }

        string type { get; }

        string author { get; }

        string description { get; }

        string category { get; }

        string packageUniqueId { get; }

        string uniqueId { get; }

        // TODO: Might need to create a wrapper `PackageSource` to account for AssetStore package info.
        PackageSource source { get; }

        IEnumerable<Error> errors { get; }

        IEnumerable<Sample> samples { get; }

        SemVersion version { get; }

        DateTime? publishedDate { get; }

        string publisherId { get; }

        DependencyInfo[] dependencies { get; }

        DependencyInfo[] resolvedDependencies { get; }

        PackageInfo packageInfo { get; }

        bool HasTag(PackageTag tag);

        bool isInstalled { get; }

        // A version is fully fetched when the information isn't derived from another version (therefore may be inaccurate)
        bool isFullyFetched { get; }

        bool isUserVisible { get; }

        bool isAvailableOnDisk { get; }

        bool isVersionLocked { get; }

        bool canBeRemoved { get; }

        bool canBeEmbedded { get; }

        bool isDirectDependency { get; }

        string localPath { get; }

        string versionString { get; }

        string versionId { get; }

        SemVersion supportedVersion { get; }

        IEnumerable<SemVersion> supportedVersions { get; }

        IEnumerable<PackageImage> images { get; }

        IEnumerable<PackageSizeInfo> sizes { get; }

        IEnumerable<PackageLink> links { get; }

        EntitlementsInfo entitlements { get; }
    }
}
