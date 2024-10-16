// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    // If a new state is added to this list, make sure to sync up the PackageItem.k_TooltipsByProgress array
    internal enum PackageState
    {
        None = 0,
        Installed,
        InstalledAsDependency,
        DownloadAvailable,
        ImportAvailable,
        Imported,
        InDevelopment,
        UpdateAvailable,
        InProgress,
        Error,
        Warning
    }
}
