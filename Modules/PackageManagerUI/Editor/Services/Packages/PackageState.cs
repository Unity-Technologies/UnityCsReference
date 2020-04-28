// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal enum PackageState
    {
        None = 0,
        Installed,
        InstalledAsDependency,
        DownloadAvailable,
        InstallAvailable,
        ImportAvailable,
        InDevelopment,
        UpdateAvailable,
        InProgress,
        Error
    }
}
