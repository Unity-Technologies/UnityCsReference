// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IPackageManagerPrefs
    {
        event Action<bool> onShowDependenciesChanged;

        event Action<bool> onShowPreviewPackagesChanged;

        bool skipRemoveConfirmation { get; set; }

        bool skipDisableConfirmation { get; set; }

        bool showPackageDependencies { get; set; }

        bool hasShowPreviewPackagesKey {get; }

        bool showPreviewPackagesFromInstalled { get; set; }

        bool showPreviewPackages { get; set; }

        bool showPreviewPackagesWarning { get; set; }

        PackageFilterTab? lastUsedPackageFilter { get; set; }
    }
}
