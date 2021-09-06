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
        string displayName { get; }

        DateTime? purchasedTime { get; }

        new IVersionList versions { get; }

        PackageState state { get; }

        PackageProgress progress { get; set; }

        IEnumerable<string> labels { get; }

        IEnumerable<PackageImage> images { get; }

        IEnumerable<PackageLink> links { get; }

        bool Is(PackageType type);

        bool isDiscoverable { get; }

        // package level errors (for upm this refers to operation errors that are separate from the package info)
        IEnumerable<UIError> errors { get; }

        bool hasEntitlements { get; }

        bool hasEntitlementsError { get; }

        // Use `PackageDatabase.AddPackageError` instead if you want to trigger `onPackagesChanged` events
        void AddError(UIError error);

        // Use `PackageDatabase.ClearPackageErrors` instead if you want to trigger `onPackagesChanged` events
        void ClearErrors(Predicate<UIError> match = null);

        IPackage Clone();

        DateTime? firstPublishedDate { get; }

        string GetDescriptor(bool isFirstLetterCapitalized = false);
    }
}
