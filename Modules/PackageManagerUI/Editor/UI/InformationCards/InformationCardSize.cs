// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal enum InformationCardSize
{
    Small,
    Medium,
    Large
}

internal static class InformationCardSizeExtensions
{
    public static string ClassName(this InformationCardSize size)
    {
        return size.ToString().ToLowerInvariant();
    }
}
