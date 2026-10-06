// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Android = Unity.Localization.Android;

namespace Unity.Localization.Editor;

// Resolves the Android application info into platform-neutral data for a writer in the platform support to emit.
// The resolution rules live here because they belong to localization; the resource layout belongs to the platform.
static class LauncherResourceBuilder
{
    // Lowest density to highest. The order is the contract the payload documents, so a writer can map position to
    // whatever its platform calls each density.
    [NoAutoStaticsCleanup] // immutable lookup table
    static readonly Android.IconDensity[] k_Densities =
    {
        Android.IconDensity.Low,
        Android.IconDensity.Medium,
        Android.IconDensity.High,
        Android.IconDensity.ExtraHigh,
        Android.IconDensity.ExtraExtraHigh,
        Android.IconDensity.ExtraExtraExtraHigh
    };

    internal static bool TryBuildAndroid(out LauncherResources resources)
    {
        resources = default;
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            return false;

        var appInfo = settings.AndroidAppInfoSetting;
        var locales = new List<LauncherLocaleResources>();
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale == null || !locale.Enabled || string.IsNullOrEmpty(locale.Code))
                continue;

            ResolveAdaptive(appInfo.AdaptiveIcons, settings, locale, out var backgrounds, out var foregrounds);
            locales.Add(new LauncherLocaleResources(
                locale.Code,
                ResolveName(appInfo.DisplayName, settings, locale),
                ResolveIconSet(appInfo.LegacyIcons, settings, locale, "launcher icon"),
                ResolveIconSet(appInfo.RoundIcons, settings, locale, "round launcher icon"),
                backgrounds,
                foregrounds));
        }

        resources = new LauncherResources(locales);
        return true;
    }

    static string ResolveName(LocalizedString name, LocalizationSettings settings, Locale locale)
    {
        if (name.IsEmpty)
            return null;

        var value = AppInfoResolver.ResolveString(name, settings, locale);
        if (value == null)
        {
            Debug.LogWarning($"Localization: no value for the application name in {locale.Identifier}, so that locale keeps the name from the Player Settings.");
            return null;
        }

        if (AppInfoResolver.IsSmart(name))
            Debug.LogWarning($"Localization: the application name uses a Smart String, which is not formatted for platform resources. {locale.Identifier} gets the unformatted text.");
        if (AppInfoResolver.IsVariantDriven(name))
            Debug.LogWarning($"Localization: the application name is variant driven, and a built resource cannot select a variant, so {locale.Identifier} gets the default value.");
        return value;
    }

    // A platform that matches the locale before the density prefers any localized icon over the unlocalized one, so
    // an incomplete set would have a device scale the wrong size. A locale contributes every density or none.
    static IReadOnlyList<string> ResolveIconSet(Android.Icons icons, LocalizationSettings settings, Locale locale, string what)
    {
        if (icons.IsEmpty())
            return null;

        var paths = new string[k_Densities.Length];
        for (var i = 0; i < k_Densities.Length; i++)
        {
            paths[i] = AppInfoResolver.ResolveTexturePath(icons.GetIcon(k_Densities[i]), settings, locale);
            if (paths[i] != null)
                continue;
            Debug.LogWarning($"Localization: the {what} for {locale.Identifier} has no {k_Densities[i]} texture, so that locale keeps the icons from the Player Settings. Assign every density to localize it.");
            return null;
        }
        return paths;
    }

    // An adaptive icon composes two layers, so a density is only usable when both resolve and the pair is reported
    // together or not at all.
    static void ResolveAdaptive(Android.AdaptiveIcons icons, LocalizationSettings settings, Locale locale,
        out IReadOnlyList<string> backgrounds, out IReadOnlyList<string> foregrounds)
    {
        backgrounds = null;
        foregrounds = null;
        if (icons.Background.IsEmpty() && icons.Foreground.IsEmpty())
            return;

        var resolvedBackgrounds = ResolveIconSet(icons.Background, settings, locale, "adaptive icon background");
        var resolvedForegrounds = ResolveIconSet(icons.Foreground, settings, locale, "adaptive icon foreground");
        if (resolvedBackgrounds == null || resolvedForegrounds == null)
            return;

        backgrounds = resolvedBackgrounds;
        foregrounds = resolvedForegrounds;
    }
}
