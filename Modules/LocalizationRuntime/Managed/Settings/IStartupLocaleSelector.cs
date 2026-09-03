// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Selects the locale to activate when the localization system initializes at startup.
/// </summary>
/// <remarks>
/// Implement this interface to control which <see cref="Locale"/> becomes the selected locale on startup. During
/// <see cref="LocalizationSettings.InitializeAsync"/> the configured selectors are evaluated in order and the first
/// non-null result is used; if every selector returns null the system falls back to
/// <see cref="LocalizationSettings.ProjectLocale"/>. Register selectors through
/// <see cref="LocalizationSettings.StartupSelectors"/>. Built-in implementations include
/// <see cref="CommandLineLocaleSelector"/>, <see cref="SystemLocaleSelector"/>, and
/// <see cref="PlayerPrefLocaleSelector"/>.
/// </remarks>
/// <example>
/// <para>A selector that picks a locale from a value read at startup.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/SaveFileLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="Locale"/>
public interface IStartupLocaleSelector
{
    /// <summary>
    /// Returns the locale to select, or null to defer to the next selector.
    /// </summary>
    /// <remarks>
    /// Look a locale up on <paramref name="settings"/> with <see cref="LocalizationSettings.GetLocale"/>. A disabled
    /// locale returned here is skipped during startup selection, so the next selector runs instead.
    /// </remarks>
    /// <param name="settings">The localization settings whose locales the selector chooses from.</param>
    /// <returns>The locale to use as the startup locale, or null to let the next selector in the chain decide.</returns>
    Locale GetStartupLocale(LocalizationSettings settings);
}

/// <summary>
/// Optional hook a startup selector implements to load what it needs before the locale is chosen.
/// </summary>
/// <remarks>
/// Implement this interface alongside <see cref="IStartupLocaleSelector"/> when the locale comes from somewhere that
/// has to be awaited, such as a cloud save or a platform API. <see cref="LocalizationSettings.InitializeAsync"/>
/// awaits <see cref="PrepareAsync"/> on every selector that implements it, after the asset providers have discovered
/// their locales and before any selector is asked to choose. Store the result and return it from
/// <see cref="IStartupLocaleSelector.GetStartupLocale"/>, which stays synchronous.
/// The three hooks run in the order <see cref="PrepareAsync"/>, then
/// <see cref="IStartupLocaleSelector.GetStartupLocale"/>, then
/// <see cref="IStartupLocaleInitialize.PostInitialize"/>.
/// </remarks>
/// <example>
/// <para>A selector that awaits a value before choosing a locale.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/AsyncLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="IStartupLocaleSelector"/>
/// <seealso cref="IStartupLocaleInitialize"/>
/// <seealso cref="LocalizationSettings.InitializeAsync"/>
public interface IStartupLocalePrepare
{
    /// <summary>
    /// Loads whatever the selector needs before it is asked for a locale.
    /// </summary>
    /// <remarks>
    /// Runs once per initialization. An exception thrown here is logged and the run continues, so the selector should
    /// leave itself in a state where <see cref="IStartupLocaleSelector.GetStartupLocale"/> can return null.
    /// </remarks>
    /// <param name="settings">The localization settings that are initializing.</param>
    /// <param name="cancellationToken">The token that signals the preparation should be canceled.</param>
    /// <returns>An <see cref="UnityEngine.Awaitable"/> that completes once the selector is ready to choose.</returns>
    Awaitable PrepareAsync(LocalizationSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional hook a startup selector implements to run code once after initialization completes.
/// </summary>
/// <remarks>
/// Implement this interface alongside <see cref="IStartupLocaleSelector"/> when a selector needs one-time setup after
/// the startup locale is chosen, for example subscribing to <see cref="LocalizationSettings.SelectedLocaleChanged"/>
/// to persist later locale changes. <see cref="LocalizationSettings.InitializeAsync"/> calls
/// <see cref="PostInitialize"/> on each selector that implements this interface once initialization has finished.
/// <see cref="PlayerPrefLocaleSelector"/> uses this hook to save the selected locale.
/// </remarks>
/// <example>
/// <para>A selector that logs the chosen locale after startup.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/LoggingLocaleSelectorExample.cs"/>
/// </example>
/// <seealso cref="IStartupLocaleSelector"/>
/// <seealso cref="LocalizationSettings"/>
public interface IStartupLocaleInitialize
{
    /// <summary>
    /// Runs one-time setup once the startup locale has been selected.
    /// </summary>
    /// <param name="settings">The localization settings instance that finished initializing.</param>
    void PostInitialize(LocalizationSettings settings);
}
