// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
namespace UnityEditor;

internal enum IconSettings
{
    // The build target has no icon settings
    None = 0,

    // The build target uses the standard icon settings UI, common for all platforms
    StandardIcons = 1 << 0,

    // The build target handles the icon settings UI itself
    CustomIcons = 1 << 1,
}

internal interface IIconPlatformProperties : IPlatformProperties
{
    // The PlayerSettingsIconsEditor.IconSectionGUI method uses this property to determine what options to display in the UI.
    IconSettings IconUISettings => IconSettings.StandardIcons;

    IReadOnlyDictionary<PlatformIconKind, PlatformIcon[]> GetRequiredPlatformIcons() => null;
    PlatformIconKind GetPlatformIconKindFromEnumValue(IconKind kind) => null;
}
