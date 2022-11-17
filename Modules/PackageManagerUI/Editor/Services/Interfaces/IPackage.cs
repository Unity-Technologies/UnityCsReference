// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackage
    {
        string uniqueId { get; }

        string name { get; }

        IEnumerable<IPackageVersion> versions { get; }
    }
}

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackage : UI.IPackage
    {
        IProduct product { get; }

        string displayName { get; }

        new IVersionList versions { get; }

        PackageState state { get; }

        PackageProgress progress { get; }

        bool isDiscoverable { get; }

        // package level errors (for upm this refers to operation errors that are separate from the package info)
        IEnumerable<UIError> errors { get; }

        bool hasEntitlements { get; }

        bool hasEntitlementsError { get; }

        string deprecationMessage { get; }

        bool isDeprecated { get; }

        bool IsInTab(PackageFilterTab tab);
    }
}
