// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Localization.Editor;

// The localized values a platform needs to describe an application while it is not running. Nothing here names a
// platform, because the writers that consume it live in the platform support assemblies and a platform-specific
// type may not sit in the monolithic editor.
readonly struct LauncherResources
{
    public IReadOnlyList<LauncherLocaleResources> Locales { get; }

    public LauncherResources(IReadOnlyList<LauncherLocaleResources> locales) => Locales = locales;
}

readonly struct LauncherLocaleResources
{
    // The locale's BCP 47 code. A writer maps it to whatever its platform names a localized resource folder.
    public string Code { get; }

    // The application name, or null when it is not localized for this locale.
    public string ApplicationName { get; }

    // Source asset paths for each icon set, one per screen density from the lowest to the highest, or null when the
    // set is not localized for this locale. A set is either complete or absent, because a platform that matches the
    // locale before the density would otherwise scale one of these in place of the unlocalized icon.
    public IReadOnlyList<string> LegacyIcons { get; }
    public IReadOnlyList<string> RoundIcons { get; }
    public IReadOnlyList<string> AdaptiveBackgrounds { get; }
    public IReadOnlyList<string> AdaptiveForegrounds { get; }

    public LauncherLocaleResources(string code, string applicationName, IReadOnlyList<string> legacyIcons,
        IReadOnlyList<string> roundIcons, IReadOnlyList<string> adaptiveBackgrounds, IReadOnlyList<string> adaptiveForegrounds)
    {
        Code = code;
        ApplicationName = applicationName;
        LegacyIcons = legacyIcons;
        RoundIcons = roundIcons;
        AdaptiveBackgrounds = adaptiveBackgrounds;
        AdaptiveForegrounds = adaptiveForegrounds;
    }

    public bool IsEmpty => ApplicationName == null && LegacyIcons == null && RoundIcons == null &&
        AdaptiveBackgrounds == null && AdaptiveForegrounds == null;
}
