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
/// <para>Selects French when it is one of the available locales.</para>
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
/// <para>Reads the locale from the command line when the player starts.</para>
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
/// Detects the locale from the device's current UI culture and matches the closest available locale.
/// </summary>
/// <remarks>
/// This selector reads the system UI culture (<see cref="CultureInfo.CurrentUICulture"/>) and walks up the culture
/// parent chain, returning the first available locale whose <see cref="LocaleIdentifier"/> matches. For example, if
/// the device culture is <c>fr-FR</c> but only <c>fr</c> is available, it selects <c>fr</c>. If no ancestor culture
/// matches, <see cref="GetStartupLocale"/> returns null. This selector is part of the default selector chain.
/// Override <see cref="GetSystemCulture"/> to supply a fixed culture in tests.
/// </remarks>
/// <example>
/// <para>Selects the locale that best matches the device language.</para>
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
        var culture = GetSystemCulture();
        while (culture != null && !string.IsNullOrEmpty(culture.Name))
        {
            var locale = settings.GetLocale(new LocaleIdentifier(culture.Name));
            if (locale != null)
                return locale;
            culture = culture.Parent;
        }
        return null;
    }

    /// <summary>
    /// Returns the system culture used to detect the startup locale.
    /// </summary>
    /// <remarks>
    /// By default this returns <see cref="CultureInfo.CurrentUICulture"/>. Override it in a derived selector to supply
    /// a fixed culture, which is useful for testing locale detection without changing the device settings.
    /// </remarks>
    /// <returns>The culture used to detect the locale; by default the device's current UI culture.</returns>
    /// <example>
    /// <para>Overrides the detected culture to always report German.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/SystemCultureOverrideExample.cs"/>
    /// </example>
    protected virtual CultureInfo GetSystemCulture() => CultureInfo.CurrentUICulture;
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
/// <para>Reads the previously saved locale when the game starts.</para>
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
