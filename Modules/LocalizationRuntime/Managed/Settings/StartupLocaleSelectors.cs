// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Selects a fixed locale by its identifier when that locale is available.
/// </summary>
/// <remarks>
/// Use this selector to force a specific locale at startup regardless of the device or command line. Set
/// <see cref="LocaleId"/> to the <see cref="LocaleIdentifier"/> of the locale to select. If no available locale
/// matches the identifier, <see cref="GetStartupLocale"/> returns null and the next selector in
/// <see cref="LocalizationSettings.StartupSelectors"/> runs.
/// </remarks>
/// <example>
/// Selects French when it is one of the available locales.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/SpecificLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="IStartupLocaleSelector"/>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="Locale"/>
[Serializable]
public class SpecificLocaleSelector : IStartupLocaleSelector
{
    [SerializeField] LocaleIdentifier m_LocaleId;

    /// <summary>
    /// The identifier of the locale to select.
    /// </summary>
    public LocaleIdentifier LocaleId
    {
        get => m_LocaleId;
        set => m_LocaleId = value;
    }

    /// <inheritdoc/>
    public Locale GetStartupLocale(LocalizationSettings settings) => settings?.GetLocale(m_LocaleId);
}

/// <summary>
/// Selects the locale from a command line argument, such as -language=fr.
/// </summary>
/// <remarks>
/// On startup this selector scans the command line for the prefix set by <see cref="CommandLineArgument"/>
/// (<c>-language=</c> by default) and selects the locale whose <see cref="LocaleIdentifier"/> matches the value that
/// follows, for example <c>-language=fr</c>. If the argument is absent or no available locale matches,
/// <see cref="GetStartupLocale"/> returns null and the next selector in
/// <see cref="LocalizationSettings.StartupSelectors"/> runs. This selector is part of the default selector chain,
/// ahead of <see cref="SystemLocaleSelector"/>.
/// </remarks>
/// <example>
/// Reads the locale from the command line when the player starts.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/CommandLineLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="IStartupLocaleSelector"/>
/// <seealso cref="LocalizationSettings"/>
[Serializable]
public class CommandLineLocaleSelector : IStartupLocaleSelector
{
    [SerializeField] string m_CommandLineArgument = "-language=";

    /// <summary>
    /// The command line argument prefix used to assign the locale.
    /// </summary>
    /// <remarks>The default prefix is <c>-language=</c>; the text after the prefix is used as the locale code.</remarks>
    public string CommandLineArgument
    {
        get => m_CommandLineArgument;
        set => m_CommandLineArgument = value;
    }

    /// <inheritdoc/>
    public Locale GetStartupLocale(LocalizationSettings settings)
    {
        if (settings == null || string.IsNullOrEmpty(m_CommandLineArgument))
            return null;
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith(m_CommandLineArgument, StringComparison.OrdinalIgnoreCase))
                return settings.GetLocale(new LocaleIdentifier(arg.Substring(m_CommandLineArgument.Length)));
        }
        return null;
    }
}

/// <summary>
/// Detects the language the device is set to and matches the closest available locale.
/// </summary>
/// <remarks>
/// This selector first tries <see cref="GetSystemCulture"/>, which reports the language the user asked the operating
/// system for. Apple platforms, Android and WebGL give that as a language tag carrying the region and the script,
/// such as <c>pt-BR</c> or <c>zh-Hant-TW</c>; elsewhere it falls back to <see cref="CultureInfo.CurrentUICulture"/>.
/// When no locale matches it tries <see cref="UnityEngine.Application.systemLanguage"/>, which every platform
/// reports but which carries the language alone.
/// Each answer is matched against <see cref="LocaleIdentifier"/> and then up the culture parent chain, so a device
/// set to <c>fr-FR</c> selects <c>fr</c> when only <c>fr</c> is available. Disabled locales are passed over, so a
/// disabled match does not stop the search. If nothing matches,
/// <see cref="GetStartupLocale"/> returns null and the next selector in
/// <see cref="LocalizationSettings.StartupSelectors"/> runs. This selector is part of the default selector chain.
/// Override <see cref="GetSystemCulture"/> to supply a fixed culture in tests.
/// </remarks>
/// <example>
/// Selects the locale that best matches the device language.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/SystemLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="IStartupLocaleSelector"/>
/// <seealso cref="LocalizationSettings"/>
[Serializable]
public class SystemLocaleSelector : IStartupLocaleSelector
{
    /// <inheritdoc/>
    public Locale GetStartupLocale(LocalizationSettings settings)
    {
        if (settings == null)
            return null;
        return FindLocale(settings, GetSystemCulture())
            ?? FindLocale(settings, SystemLocale.GetLanguageCultureCode((int)GetApplicationSystemLanguage()));
    }

    static Locale FindLocale(LocalizationSettings settings, string code)
    {
        if (string.IsNullOrEmpty(code))
            return null;
        var locale = settings.GetLocale(new LocaleIdentifier(code));
        if (locale is { Enabled: true })
            return locale;
        try
        {
            return FindLocale(settings, CultureInfo.GetCultureInfo(code));
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    // A disabled match must not end the search, or it would hide the remaining sources from GetStartupLocale.
    static Locale FindLocale(LocalizationSettings settings, CultureInfo culture)
    {
        while (culture != null && !string.IsNullOrEmpty(culture.Name))
        {
            var locale = settings.GetLocale(new LocaleIdentifier(culture.Name));
            if (locale is { Enabled: true })
                return locale;
            culture = culture.Parent;
        }
        return null;
    }

    /// <summary>
    /// Returns the culture used to detect the startup locale.
    /// </summary>
    /// <remarks>
    /// By default this returns the language the user asked the operating system for, which Apple platforms, Android
    /// and WebGL report as a language tag carrying the region and the script. On platforms that cannot report one it
    /// returns <see cref="CultureInfo.CurrentUICulture"/> instead. Override it in a derived selector to supply a fixed
    /// culture, which replaces both and is useful for testing locale detection without changing the device settings.
    /// </remarks>
    /// <returns>The culture used to detect the locale; by default the language the device is set to.</returns>
    /// <example>
    /// Overrides the detected culture to always report German.
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/SystemCultureOverrideExample.cs"/>
    /// </example>
    protected virtual CultureInfo GetSystemCulture()
    {
        // A tag can carry subtags the culture database does not know, such as the -u- extensions Android reports,
        // so drop them one at a time rather than abandoning the whole tag.
        for (var tag = GetPlatformLanguageCode(); !string.IsNullOrEmpty(tag); tag = TrimLastSubtag(tag))
        {
            try
            {
                return CultureInfo.GetCultureInfo(tag);
            }
            catch (CultureNotFoundException)
            {
            }
        }
        return GetCurrentUICulture();
    }

    static string TrimLastSubtag(string tag)
    {
        var separator = tag.LastIndexOf('-');
        return separator > 0 ? tag.Substring(0, separator) : string.Empty;
    }

    // Empty on platforms with no native implementation, which falls GetSystemCulture back to the managed culture.
    internal virtual string GetPlatformLanguageCode() => SystemLocale.GetPreferredLanguageTag();

    internal virtual CultureInfo GetCurrentUICulture() => CultureInfo.CurrentUICulture;

    internal virtual SystemLanguage GetApplicationSystemLanguage() => Application.systemLanguage;
}

/// <summary>
/// Stores the last selected locale in PlayerPrefs and restores it on startup.
/// </summary>
/// <remarks>
/// On startup this selector reads the locale code saved under <see cref="PlayerPreferenceKey"/> from
/// <see cref="PlayerPrefs"/> and selects the matching locale. Because it also implements
/// <see cref="IStartupLocaleInitialize"/>, it subscribes to <see cref="LocalizationSettings.SelectedLocaleChanged"/>
/// after initialization and writes the new code whenever the locale changes, so the choice persists across sessions.
/// If no code is stored or no available locale matches, <see cref="GetStartupLocale"/> returns null and the next
/// selector runs. Add it to <see cref="LocalizationSettings.StartupSelectors"/> to enable persistence.
/// </remarks>
/// <example>
/// Reads the previously saved locale when the game starts.
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/PlayerPrefLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="IStartupLocaleSelector"/>
/// <seealso cref="IStartupLocaleInitialize"/>
/// <seealso cref="LocalizationSettings"/>
[Serializable]
public class PlayerPrefLocaleSelector : IStartupLocaleSelector, IStartupLocaleInitialize
{
    [SerializeField] string m_PlayerPreferenceKey = "selected-locale";

    /// <summary>
    /// The PlayerPrefs key used to store the selected locale code.
    /// </summary>
    /// <remarks>The locale code is stored in <see cref="PlayerPrefs"/> under this key. The default key is <c>selected-locale</c>.</remarks>
    public string PlayerPreferenceKey
    {
        get => m_PlayerPreferenceKey;
        set => m_PlayerPreferenceKey = value;
    }

    /// <inheritdoc/>
    public Locale GetStartupLocale(LocalizationSettings settings)
    {
        if (settings == null || !PlayerPrefs.HasKey(m_PlayerPreferenceKey))
            return null;
        var code = PlayerPrefs.GetString(m_PlayerPreferenceKey);
        return string.IsNullOrEmpty(code) ? null : settings.GetLocale(new LocaleIdentifier(code));
    }

    /// <inheritdoc/>
    public void PostInitialize(LocalizationSettings settings)
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    void OnSelectedLocaleChanged(Locale locale)
    {
        if (locale != null)
            PlayerPrefs.SetString(m_PlayerPreferenceKey, locale.Code);
    }
}
