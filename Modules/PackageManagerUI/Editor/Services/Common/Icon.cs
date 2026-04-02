// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal enum Icon
{
    None,

    InProjectPage,
    UpdatesPage,
    SamplesPage,
    UnityRegistryPage,
    MyAssetsPage,
    BuiltInPage,
    ServicesPage,
    MyRegistriesPage,

    Refresh,
    Folder,
    Installed,
    Download,
    Import,
    Customized,
    Pause,
    Resume,
    Cancel,
    UpdateAvailable,

    Info,
    Error,
    Warning,
    Success,

    Verified,

    PullDown,

    RegistryErrorLarge,
    PackageWarningLarge,
    PackageOptionLarge,
    PackageErrorLarge,
    RestrictedErrorLarge
}

internal static class IconExtension
{
    private const string k_IconClassNamePrefix = "icon";
    public static string ClassName(this Icon icon) => k_IconClassNamePrefix + icon;
}
