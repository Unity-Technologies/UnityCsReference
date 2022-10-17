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

        // versionString and versionId are the same for UpmPackage but difference for in the case Asset Store packages:
        // versionString - something that looks like `1.0.2` or `1.0a`
        // versionId     - the unique numeric id that is used in the Asset Store backend that looks like `12345`
        // to avoid confusing external developers, we only expose `versionString` in the public API and not `versionId`
        string versionString { get; }

        string uniqueId { get; }

        string packageUniqueId { get; }

        IPackage package { get; }

        bool isInstalled { get; }
    }
}

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageVersion : UI.IPackageVersion
    {
        new IPackage package { get; set; }

        string author { get; }

        // Note that localReleaseNotes here is different from IPackage.latestReleaseNotes
        // localReleaseNotes is always from the locally downloaded .unitypackage, while
        // latestReleaseNotes is always from the productInfo we fetch from the server.
        string localReleaseNotes { get; }

        // Note that description here is different from IPackage.productDescription
        // description here is the description for this specific package version (from PackageInfo)
        // productDescription is the product description we fetch from the Asset Store server
        string description { get; }

        string category { get; }

        IDictionary<string, string> categoryLinks { get; }

        IEnumerable<UIError> errors { get; }

        bool hasEntitlements { get; }

        bool hasEntitlementsError { get; }

        SemVersion? version { get; }

        string versionInManifest { get; }

        string versionId { get; }

        DateTime? publishedDate { get; }

        DependencyInfo[] dependencies { get; }

        DependencyInfo[] resolvedDependencies { get; }

        bool HasTag(PackageTag tag);

        // A version is fully fetched when the information isn't derived from another version (therefore may be inaccurate)
        bool isFullyFetched { get; }

        bool isAvailableOnDisk { get; }

        bool isDirectDependency { get; }

        bool isUnityPackage { get; }

        string localPath { get; }

        SemVersion? supportedVersion { get; }

        IEnumerable<SemVersion> supportedVersions { get; }

        IEnumerable<PackageSizeInfo> sizes { get; }

        EntitlementsInfo entitlements { get; }

        bool IsDifferentVersionThanRequested { get; }

        bool IsRequestedButOverriddenVersion { get; }

        bool isRegistryPackage { get; }

        bool isFromScopedRegistry { get; }
    }
}
