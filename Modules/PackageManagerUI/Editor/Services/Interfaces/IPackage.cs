// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackage
    {
        string uniqueId { get; }

        string name { get; }

        string displayName { get; }

        IEnumerable<IPackageVersion> versions { get; }

        IEnumerable<IPackageVersion> keyVersions { get; }

        IPackageVersion installedVersion { get; }

        IPackageVersion latestVersion { get; }

        IPackageVersion latestPatch { get; }

        // the recommended version to install or update to
        IPackageVersion recommendedVersion { get; }

        // the primary version is most important version that we want to show to the user
        // it will be the default that will be displayed if no versions are selected
        IPackageVersion primaryVersion { get; }

        PackageState state { get; }
        bool isDiscoverable { get; }

        // package level errors (for upm this refers to operation errors that are separate from the package info)
        IEnumerable<Error> errors { get; }

        void AddError(Error error);

        void ClearErrors();
    }
}
