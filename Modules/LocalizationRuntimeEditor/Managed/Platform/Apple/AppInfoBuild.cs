// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Localization.Apple;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Apple;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Localization.Editor.Apple;

// Resolves the Apple app info once the player is built, then hands it to the Apple platform support to write into
// the generated Xcode project. The property list and Xcode types are not reachable from an engine module, so the
// writing half lives behind ILocalizedAppInfoWriter.
class AppInfoBuild : IPostprocessBuildWithReport
{
    [NoAutoStaticsCleanup] // immutable lookup table
    static readonly (string key, Func<AppInfo, LocalizedString> value)[] k_Keys =
    {
        ("CFBundleName", info => info.ShortName),
        ("CFBundleDisplayName", info => info.DisplayName),
        ("NSCameraUsageDescription", info => info.CameraUsageDescription),
        ("NSMicrophoneUsageDescription", info => info.MicrophoneUsageDescription),
        ("NSLocationWhenInUseUsageDescription", info => info.LocationUsageDescription),
        ("NSUserTrackingUsageDescription", info => info.UserTrackingUsageDescription)
    };

    public int callbackOrder => 1;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (!IsApple(report.summary.platform))
            return;

        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            return;

        var appInfo = settings.AppleAppInfoSetting;
        var payload = BuildPayload(settings, appInfo);
        if (payload.Locales.Count == 0)
            return;

        var writer = FindWriter(report.summary.platform);
        if (writer == null)
        {
            Debug.LogWarning($"Localization: the Apple App Info cannot be written for {report.summary.platform} because the platform support that writes it is not installed. The built application keeps the name and prompts from the Player Settings.");
            return;
        }

        try
        {
            if (!writer.Write(report.summary.outputPath, report.summary.platform, payload))
                Debug.LogWarning($"Localization: no Unity Xcode project was found in '{report.summary.outputPath}', so the Apple App Info was not written. On macOS the build has to create an Xcode project for the property list to be localized.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Localization: failed to write the Apple App Info into the Xcode project. The built application keeps the name and prompts from the Player Settings.\n{e}");
        }
    }

    static LocalizedAppInfo BuildPayload(LocalizationSettings settings, AppInfo appInfo)
    {
        var locales = new List<LocalizedAppInfoLocale>();
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale == null || !locale.Enabled)
                continue;

            var values = new Dictionary<string, string>();
            foreach (var (key, select) in k_Keys)
            {
                var reference = select(appInfo);
                if (reference.IsEmpty)
                    continue;

                var value = AppInfoResolver.ResolveString(reference, settings, locale);
                if (value == null)
                {
                    Debug.LogWarning($"Localization: no value for the Apple {key} in {locale.Identifier}, so that locale falls back to the Player Settings value.");
                    continue;
                }

                if (AppInfoResolver.IsSmart(reference))
                    Debug.LogWarning($"Localization: the Apple {key} uses a Smart String, which is not formatted for a property list. {locale.Identifier} gets the unformatted text.");

                values[key] = value;
            }

            if (values.Count > 0)
                locales.Add(new LocalizedAppInfoLocale(AppleLocaleCode(locale.Identifier), values));
        }

        return new LocalizedAppInfo(DevelopmentRegion(settings), locales);
    }

    static string DevelopmentRegion(LocalizationSettings settings)
    {
        var project = settings.ProjectLocale;
        if (project != null)
            return AppleLocaleCode(project.Identifier);
        foreach (var locale in settings.AvailableLocales)
        {
            if (locale != null && locale.Enabled)
                return AppleLocaleCode(locale.Identifier);
        }
        return null;
    }

    // Apple names an .lproj folder with the BCP 47 code, so the hyphens stay as the locale writes them.
    static string AppleLocaleCode(LocaleIdentifier locale) => locale.Code;

    static bool IsApple(BuildTarget target) =>
        target is BuildTarget.iOS or BuildTarget.tvOS or BuildTarget.StandaloneOSX or BuildTarget.VisionOS;

    // Several Apple platform supports can be installed together, and the order TypeCache returns them in says
    // nothing about which target they serve, so each writer is asked.
    static ILocalizedAppInfoWriter FindWriter(BuildTarget target)
    {
        foreach (var type in TypeCache.GetTypesDerivedFrom<ILocalizedAppInfoWriter>())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;
            if (Activator.CreateInstance(type) is ILocalizedAppInfoWriter writer && writer.CanWrite(target))
                return writer;
        }
        return null;
    }
}
