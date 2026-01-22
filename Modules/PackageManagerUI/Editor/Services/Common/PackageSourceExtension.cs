// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal static class PackageSourceExtension
{
    public static string GetDisplayName(this PackageSource source)
    {
        return source switch
        {
            PackageSource.LocalTarball => L10n.Tr("Tarball"),
            PackageSource.BuiltIn => L10n.Tr("Built-in"),
            PackageSource.Embedded => L10n.Tr("Custom"),
            _ => L10n.Tr(source.ToString())
        };
    }
}
