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

        string displayName { get; }

        DateTime? purchasedTime { get; }

        IVersionList versions { get; }

        PackageState state { get; }

        PackageProgress progress { get; set; }

        IEnumerable<string> labels { get; }

        IEnumerable<PackageImage> images { get; }

        IEnumerable<PackageLink> links { get; }

        bool Is(PackageType type);

        bool isDiscoverable { get; }

        // package level errors (for upm this refers to operation errors that are separate from the package info)
        IEnumerable<UIError> errors { get; }

        void AddError(UIError error);

        void ClearErrors(Predicate<UIError> match = null);

        IPackage Clone();

        DateTime? firstPublishedDate { get; }
    }
}
