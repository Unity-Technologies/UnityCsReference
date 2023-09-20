// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor;

[Flags]
internal enum IconOption
{
    // A build target uses this value if it has its own mechanism for icon settings.
    None = 0,

    // A build target uses this value if it has no icon settings.
    NotApplicable = 1 << 0,

    // A build target uses this value if it has the usual icons, such as legacy icons.
    StandardIcons = 1 << 1,
}

internal interface IIconPlatformProperties : IPlatformProperties
{
    // The PlayerSettingsIconsEditor.IconSectionGUI method uses this property to determine what options to display in the UI.
    // This is a flags enumeration since there are several overlapping options.
    IconOption IconOptions => IconOption.StandardIcons;

    // The PlayerSettings class uses this method to get the platform icon provider and uses fallback when the provider is null.
    IPlatformIconProvider GetPlatformIconProvider() => null;
}
