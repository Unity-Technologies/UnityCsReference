// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageManagerPrefs
    {
        bool dismissPreviewPackagesInUse { get; set; }

        bool skipRemoveConfirmation { get; set; }

        bool skipDisableConfirmation { get; set; }

        PackageFilterTab? lastUsedPackageFilter { get; set; }

        bool dependenciesExpanded { get; set; }

        bool samplesExpanded { get; set; }
    }
}
