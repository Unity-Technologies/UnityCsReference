// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IVersionList : IEnumerable<IPackageVersion>
    {
        IEnumerable<IPackageVersion> key { get; }

        IPackageVersion installed { get; }

        IPackageVersion latest { get; }

        IPackageVersion importAvailable { get; }

        IPackageVersion imported { get; }

        // the version recommended by Unity, this should only be set for Unity packages
        IPackageVersion recommended { get; }

        IPackageVersion suggestedUpdate { get; }

        // the primary version is the most important version that we want to show to the user
        // it is the default version that will be displayed in the list as well as in the details
        IPackageVersion primary { get; }

        IPackageVersion GetUpdateTarget(IPackageVersion version);

        int numUnloadedVersions { get; }
    }
}
