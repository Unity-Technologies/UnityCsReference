// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IVersionList : IEnumerable<IPackageVersion>
    {
        IEnumerable<IPackageVersion> key { get; }

        IPackageVersion installed { get; }

        IPackageVersion latest { get; }

        IPackageVersion importAvailable { get; }

        // the recommended version to install or update to
        IPackageVersion recommended { get; }

        // the primary version is most important version that we want to show to the user
        // it will be the default that will be displayed if no versions are selected
        IPackageVersion primary { get; }

        // This refers to the number of versions that's unloaded from the memory for performance gains
        int numUnloadedVersions { get; }
    }
}
