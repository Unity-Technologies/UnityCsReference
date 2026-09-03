// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Localization.Providers;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization;

/// <summary>
/// The runtime settings for localization: the available locales, the selected locale, and the resource database.
/// </summary>
/// <remarks>
/// This is the central entry point for the standalone localization runtime, and it works with no package installed.
/// One active instance is exposed through <see cref="Instance"/>; its <see cref="Database"/> (a
/// <see cref="Unity.Localization.ResourceDatabase"/>) resolves localized strings and assets. Locales are held here
/// as plain <see cref="Locale"/> objects, and everything else refers to a locale by its <see cref="LocaleIdentifier"/>
/// code, resolved through <see cref="GetLocale"/>. The locale list is polymorphic (<c>[SerializeReference]</c>), so
/// locale subclasses such as a pseudo-locale can be added. Register locales with <see cref="AddLocale"/>; asset
/// providers can contribute more during <see cref="InitializeAsync"/>. The startup locale is decided by the
/// <see cref="StartupSelectors"/>. Call <see cref="InitializeAsync"/> to load locales and select the startup locale.
/// </remarks>
/// <example>
/// <para>Initializes localization and switches to a specific locale.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/InitializeAndSwitchLocaleExample.cs"/>
/// </example>
/// <seealso cref="Unity.Localization.ResourceDatabase"/>
/// <seealso cref="Locale"/>
/// <seealso cref="LocaleIdentifier"/>
/// <seealso cref="IStartupLocaleSelector"/>
public partial class LocalizationSettings : ScriptableObject
{
    [AutoStaticsCleanup] // the wrapper type is reloadable; Initialize() re-registers the active settings
    static LocalizationSettings s_Instance;

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static void ResetStaticsForPlayMode()
    {
        SelectedLocaleChanged = null;
        InitializationCompleted = null;
        if (s_Instance != null)
        {
            // An initialization run still in flight belongs to the previous session; abandon it.
            s_Instance.m_InitGeneration++;
            s_Instance.m_InitCts?.Cancel();
            s_Instance.m_Initialized = false;
            s_Instance.m_Initializing = false;
            s_Instance.m_Preparing = false;
            s_Instance.m_InitializationRequested = false;
            s_Instance.CompleteInitWaiters();
            s_Instance.m_SelectedLocaleCode = s_Instance.m_ProjectLocaleCode;
        }
        s_Instance = null;
    }

    /// <summary>
    /// The key under which the active settings are stored as an editor build config object.
    /// </summary>
    public const string ConfigName = "com.unity.localization.runtime.settings";

    [SerializeReference] List<Locale> m_AvailableLocales = new();
    [SerializeField] string m_ProjectLocaleCode;
    [SerializeReference] ResourceDatabase m_ResourceDatabase = new();
    [SerializeReference] List<IStartupLocaleSelector> m_StartupSelectors = new() { new CommandLineLocaleSelector(), new SystemLocaleSelector() };
    [SerializeField] LoadingPreference m_PreferredLoading = LoadingPreference.Asynchronous;
#pragma warning disable CS0649
    [SerializeField] bool m_DeferInitialization;
#pragma warning restore CS0649

    string m_SelectedLocaleCode;
    bool m_Initialized;
    bool m_Initializing;
    bool m_Preparing;
    bool m_InitializationRequested;
#pragma warning disable CS0649
    int m_InitGeneration;
#pragma warning restore CS0649
    CancellationTokenSource m_InitCts;
    List<AwaitableCompletionSource> m_InitWaiters;

    /// <summary>
    /// Raised after the selected locale changes.
    /// </summary>
    /// <remarks>
    /// Subscribe to this static event to update UI or reload content when the user switches language. It is invoked
    /// after <see cref="SelectedLocale"/> has changed, both during startup selection and on later changes; the new
    /// locale is passed to the handler. Because it is static, unsubscribe when your object is destroyed to avoid
    /// leaks. To run code once when initialization finishes instead, use <see cref="InitializationCompleted"/>.
    /// </remarks>
    /// <example>
    /// <para>Refreshes a label whenever the locale changes.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/SelectedLocaleChangedExample.cs"/>
    /// </example>
    /// <seealso cref="SelectedLocale"/>
    /// <seealso cref="InitializationCompleted"/>
    [AutoStaticsCleanup] // holds subscriber delegates; mirrors ResetStaticsForPlayMode()
    public static event Action<Locale> SelectedLocaleChanged;

    /// <summary>
    /// Raised once after initialization completes.
    /// </summary>
    /// <remarks>
    /// Subscribe to this static event to run setup that depends on locales being loaded and the startup locale being
    /// selected. It is invoked once, when <see cref="InitializeAsync"/> finishes. If initialization has already
    /// completed when you subscribe, your handler is not called, so check <see cref="IsInitialized"/> first. Because
    /// it is static, unsubscribe when your object is destroyed.
    /// </remarks>
    /// <example>
    /// <para>Runs code once localization is ready.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/InitializationCompletedExample.cs"/>
    /// </example>
    /// <seealso cref="InitializeAsync"/>
    /// <seealso cref="SelectedLocaleChanged"/>
    [AutoStaticsCleanup] // holds subscriber delegates; mirrors ResetStaticsForPlayMode()
    public static event Action InitializationCompleted;

    /// <summary>
    /// The active settings instance, or null when none is registered.
    /// </summary>
    /// <remarks>
    /// The localization system uses this single active instance for all lookups. It is set automatically when a
    /// settings object is enabled, and static members such as <see cref="SelectedLocale"/> read from it. Use
    /// <see cref="HasSettings"/> to check whether an instance is registered without dereferencing it.
    /// </remarks>
    public static LocalizationSettings Instance
    {
        get => s_Instance;
        set => s_Instance = value;
    }

    /// <summary>
    /// Whether an active settings instance is registered.
    /// </summary>
    public static bool HasSettings => s_Instance != null;

    /// <summary>
    /// The active resource database, or null when no settings instance is registered.
    /// </summary>
    /// <remarks>Convenience accessor for the <see cref="Database"/> of the active <see cref="Instance"/>; resolves localized strings and assets.</remarks>
    public static ResourceDatabase ResourceDatabase => s_Instance != null ? s_Instance.m_ResourceDatabase : null;

    /// <summary>
    /// The default loading behaviour used by the reference accessors and their change events.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="LoadingPreference.Asynchronous"/>. When set to <see cref="LoadingPreference.Synchronous"/>,
    /// the event-driven accessors (<see cref="LocalizedString.GetLocalizedStringAsync"/>,
    /// <see cref="LocalizedAsset{TObject}.GetLocalizedAssetAsync"/>, and <see cref="LocalizedTable.GetTableAsync"/>)
    /// resolve synchronously when the value is available, and fall back to an asynchronous load when it is not. When no
    /// settings instance is registered, this reads as <see cref="LoadingPreference.Asynchronous"/>.
    /// </remarks>
    /// <example>
    /// <para>Make localized references resolve synchronously by default.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/PreferredLoadingExample.cs"/>
    /// </example>
    /// <seealso cref="LoadingPreference"/>
    public static LoadingPreference PreferredLoading
    {
        get => s_Instance != null ? s_Instance.m_PreferredLoading : LoadingPreference.Asynchronous;
        set { if (s_Instance != null) s_Instance.m_PreferredLoading = value; }
    }

    /// <summary>
    /// The locales registered on this settings instance.
    /// </summary>
    /// <remarks>
    /// This includes disabled locales. Modify the set with <see cref="AddLocale"/> and <see cref="RemoveLocale"/>.
    /// Only enabled locales (see <see cref="Locale.Enabled"/>) are offered for startup selection at runtime.
    /// </remarks>
    public IReadOnlyList<Locale> AvailableLocales => m_AvailableLocales;

    /// <summary>
    /// The startup locale selectors, evaluated in order to pick the selected locale on initialization.
    /// </summary>
    /// <remarks>
    /// During <see cref="InitializeAsync"/> these <see cref="IStartupLocaleSelector"/> instances are evaluated in
    /// order and the first non-null result becomes the selected locale, falling back to <see cref="ProjectLocale"/>.
    /// The default chain is a <see cref="CommandLineLocaleSelector"/> followed by a <see cref="SystemLocaleSelector"/>.
    /// </remarks>
    public List<IStartupLocaleSelector> StartupSelectors => m_StartupSelectors;

    /// <summary>
    /// Whether the system waits for an explicit call to <see cref="InitializeAsync"/> before it initializes.
    /// </summary>
    /// <remarks>
    /// Off by default, so the first value resolved starts initialization. Turn it on, through Project Settings >
    /// Localization, when the startup locale depends on something that loads first, such as a save file or a
    /// platform sign-in. Call <see cref="InitializeAsync"/> once the data is ready; it lifts the block for the rest
    /// of the session.
    /// While it is on, an awaited read simply waits and resolves once initialization runs. A synchronous read cannot
    /// wait, so it throws instead, and the stack trace names what asked too early. Nothing starts the run but you,
    /// which is the point: the alternative is loading the wrong language and reloading every table.
    /// Note that an awaited read never completes if initialization is never asked for.
    /// This applies to players and to Play Mode only. In the Editor outside Play Mode the system still initializes on
    /// demand, so the table windows and inspectors keep working with no game running to ask for a value.
    /// </remarks>
    /// <example>
    /// <para>Initialize localization only once a save file has loaded.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/DeferInitializationExample.cs"/>
    /// </example>
    /// <seealso cref="InitializeAsync"/>
    /// <seealso cref="IsInitialized"/>
    public bool DeferInitialization => m_DeferInitialization;
    /// <summary>
    /// The selector that chose the startup locale, or null when the chain fell through to the project locale.
    /// </summary>
    /// <remarks>
    /// Set during <see cref="InitializeAsync"/>. Read it to tell whether a user override was applied at startup or
    /// whether the locale came from the project default. Pair it with <see cref="EvaluateStartupLocale"/> to show
    /// both the chosen locale and the one that would have been chosen without the override.
    /// </remarks>
    /// <seealso cref="EvaluateStartupLocale"/>
    /// <seealso cref="StartupSelectors"/>
    public IStartupLocaleSelector StartupSelector { get; private set; }
    /// <summary>
    /// Whether initialization has completed.
    /// </summary>
    /// <remarks>Becomes true once <see cref="InitializeAsync"/> has loaded the locales and selected the startup locale. Check it before subscribing to <see cref="InitializationCompleted"/>.</remarks>
    public bool IsInitialized => m_Initialized;

    /// <summary>
    /// The default project locale used when no other locale is selected.
    /// </summary>
    /// <remarks>
    /// This locale is the fallback that <see cref="InitializeAsync"/> selects when none of the
    /// <see cref="StartupSelectors"/> returns a locale, and the value <see cref="SelectedLocale"/> falls back to.
    /// Setting it stores the locale's code; getting it resolves that code through <see cref="GetLocale"/>, so the
    /// locale must be one of the <see cref="AvailableLocales"/>.
    /// </remarks>
    public Locale ProjectLocale
    {
        get => GetLocale(m_ProjectLocaleCode);
        set => m_ProjectLocaleCode = value != null ? value.Code : null;
    }

    /// <summary>
    /// The resource database that resolves localized content for this instance.
    /// </summary>
    /// <remarks>Resolves localized strings and assets. The static <c>ResourceDatabase</c> property is a shortcut to this database on the active <see cref="Instance"/>.</remarks>
    public ResourceDatabase Database => m_ResourceDatabase;

    /// <summary>
    /// The asset provider used as the default when creating new localized content.
    /// </summary>
    /// <remarks>
    /// This is the entry in the <see cref="Database"/>'s <see cref="AssetProvider"/> chain chosen as the default for
    /// new content, or null until one is selected during setup. It is backed by <see cref="AssetProvider.Selected"/>,
    /// so it stays valid when a provider is removed from the chain.
    /// </remarks>
    public IAssetProvider SelectedProvider
    {
        get => m_ResourceDatabase?.AssetProvider?.Selected;
        set { if (m_ResourceDatabase?.AssetProvider != null) m_ResourceDatabase.AssetProvider.Selected = value; }
    }

    /// <summary>
    /// Returns the Smart formatter used to format Smart String entries.
    /// </summary>
    /// <remarks>
    /// Returns the default Smart formatter, <see cref="Smart.Default"/>. The resource database uses this formatter when
    /// resolving Smart entries.
    /// </remarks>
    /// <returns>The default Smart formatter.</returns>
    /// <example>
    /// <para>Formats a value with the active Smart formatter.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetSmartFormatterExample.cs"/>
    /// </example>
    public SmartFormatter GetSmartFormatter() => Smart.Default;

    /// <summary>
    /// The currently selected locale, or null when none is selected.
    /// </summary>
    /// <remarks>
    /// Reading this returns the active locale, falling back to <see cref="ProjectLocale"/> when no explicit
    /// selection has been made. Setting it changes the locale and raises <see cref="SelectedLocaleChanged"/>. The
    /// value is stored as a locale code, so the assigned locale should be one of the <see cref="AvailableLocales"/>.
    /// </remarks>
    public static Locale SelectedLocale
    {
        get
        {
            if (s_Instance == null)
                return null;
            return s_Instance.GetLocale(s_Instance.m_SelectedLocaleCode)
                ?? s_Instance.GetLocale(s_Instance.m_ProjectLocaleCode);
        }
        set
        {
            if (s_Instance != null)
                s_Instance.SetSelectedLocale(value);
        }
    }

    /// <summary>
    /// Registers a locale as available, deduplicated by locale code.
    /// </summary>
    /// <remarks>
    /// Adds the locale to <see cref="AvailableLocales"/> unless a locale with the same <see cref="LocaleIdentifier"/>
    /// is already registered, in which case the call is ignored. A null locale is ignored. Use
    /// <see cref="RemoveLocale"/> to remove a locale.
    /// </remarks>
    /// <param name="locale">The locale to register as available.</param>
    /// <example>
    /// <para>Registers a French locale.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/AddLocaleExample.cs"/>
    /// </example>
    public void AddLocale(Locale locale)
    {
        if (locale == null || GetLocale(locale.Identifier) != null)
            return;
        m_AvailableLocales.Add(locale);
    }

    /// <summary>
    /// Removes a locale from the available set.
    /// </summary>
    /// <remarks>
    /// Removes the exact locale instance from <see cref="AvailableLocales"/>. A null locale is ignored.
    /// </remarks>
    /// <param name="locale">The locale to remove from the available set.</param>
    /// <example>
    /// <para>Removes a previously registered locale.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/RemoveLocaleExample.cs"/>
    /// </example>
    public void RemoveLocale(Locale locale)
    {
        if (locale != null)
            m_AvailableLocales.Remove(locale);
    }

    /// <summary>
    /// Removes any null entries from the available locales.
    /// </summary>
    /// <remarks>
    /// Null entries can appear in <see cref="AvailableLocales"/> after a serialized locale subclass is deleted or
    /// fails to deserialize. Call this to clean up the list; it does not affect valid locales.
    /// </remarks>
    /// <example>
    /// <para>Removes null locale entries from the settings.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/CompactLocalesExample.cs"/>
    /// </example>
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void CompactLocales() => m_AvailableLocales.RemoveAll(l => l == null);

    /// <summary>
    /// Returns the available locale with the given identifier, or null.
    /// </summary>
    /// <remarks>
    /// Searches <see cref="AvailableLocales"/> for a locale whose <see cref="LocaleIdentifier"/> matches, including
    /// disabled locales. The match is case-insensitive on the locale code. Use this to turn a code into a
    /// <see cref="Locale"/> before assigning it to <see cref="SelectedLocale"/>.
    /// </remarks>
    /// <param name="identifier">The identifier of the locale to find.</param>
    /// <returns>The registered locale that matches the identifier, or null when none matches.</returns>
    /// <example>
    /// <para>Finds a locale by code and selects it.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetLocaleExample.cs"/>
    /// </example>
    public Locale GetLocale(LocaleIdentifier identifier)
    {
        if (string.IsNullOrEmpty(identifier.Code))
            return null;
        for (var i = 0; i < m_AvailableLocales.Count; i++)
        {
            if (m_AvailableLocales[i] != null && m_AvailableLocales[i].Identifier == identifier)
                return m_AvailableLocales[i];
        }
        return null;
    }

    /// <summary>
    /// Runs the startup selector chain and returns the locale it produces, without selecting it.
    /// </summary>
    /// <remarks>
    /// This is the same evaluation <see cref="InitializeAsync"/> performs, so the result is directly comparable with
    /// <see cref="SelectedLocale"/>. Pass a filter to leave selectors out: excluding the one that applies a saved
    /// user choice gives the locale the project would have started with, which is the value to show as the default
    /// and to restore when the user resets. Falls back to <see cref="ProjectLocale"/> when no selector matches.
    /// Selectors are asked again on every call, so a selector with side effects sees more than one call.
    /// </remarks>
    /// <param name="filter">Returns true for each selector to evaluate, or null to evaluate them all.</param>
    /// <returns>The locale the chain produces, or null when there is no project locale either.</returns>
    /// <example>
    /// <para>Show the saved language alongside the one the project would otherwise start in.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/EvaluateStartupLocaleExample.cs"/>
    /// </example>
    /// <seealso cref="StartupSelector"/>
    /// <seealso cref="StartupSelectors"/>
    public Locale EvaluateStartupLocale(Func<IStartupLocaleSelector, bool> filter = null)
        => SelectStartupLocale(filter, out _);
    void SetSelectedLocale(Locale locale)
    {
        var code = locale != null ? locale.Code : null;
        if (string.Equals(m_SelectedLocaleCode, code, StringComparison.OrdinalIgnoreCase))
            return;
        var previous = new LocaleIdentifier(m_SelectedLocaleCode);
        m_SelectedLocaleCode = code;
        m_ResourceDatabase?.OnLocaleChanged(previous);
        SelectedLocaleChanged?.Invoke(locale);
    }

    /// <summary>
    /// Loads the available locales and selects the startup locale, running once.
    /// </summary>
    /// <remarks>
    /// Discovers locales that the asset providers expose through <see cref="ILocaleDiscovery"/> and adds them to the
    /// registered <see cref="AvailableLocales"/>, then selects the startup locale by evaluating the
    /// <see cref="StartupSelectors"/> in order (skipping disabled locales) and falling back to
    /// <see cref="ProjectLocale"/>. Initialization runs
    /// only once; the first synchronous or asynchronous resolve through the <see cref="Database"/> triggers it
    /// automatically, so calling this explicitly is only needed when you want to await completion. When it finishes
    /// it raises <see cref="InitializationCompleted"/>.
    /// </remarks>
    /// <returns>An awaitable that completes when initialization has finished, or immediately when it has already run.</returns>
    /// <example>
    /// <para>Waits for localization to be ready before resolving a string.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/InitializeAsyncExample.cs"/>
    /// </example>
    public static Awaitable InitializeAsync()
    {
        if (s_Instance == null)
            return AwaitableUtility.Completed();
        // The explicit request is what a deferred project waits for, so it lifts the block for the rest of the session.
        s_Instance.m_InitializationRequested = true;
        return s_Instance.EnsureInitialized(mayStart: true);
    }

    // The asynchronous on-demand path. A deferred project starts the run itself, so this waits for it rather than
    // starting one; the caller is already awaiting, so it simply resolves later than it would have.
    internal static Awaitable EnsureInitializedForUse()
        => s_Instance != null ? s_Instance.EnsureInitialized(mayStart: false) : AwaitableUtility.Completed();

    // The synchronous on-demand path. It cannot wait, so starting the run is the only way it could return a value,
    // and starting it is exactly what a deferred project asked us not to do.
    internal static void RequestInitialization()
    {
        if (s_Instance == null || s_Instance.m_Initialized)
            return;
        if (s_Instance.Deferred)
            throw new InvalidOperationException(
                "Localization is waiting for LocalizationSettings.InitializeAsync, but a synchronous read asked for a " +
                "value first and cannot wait for one. The stack trace shows what asked. Await the value instead, call " +
                "InitializeAsync once the data it depends on has loaded, or turn off Defer Initialization in Project " +
                "Settings > Localization.");
        s_Instance.StartInitialization();
    }

    // Edit mode keeps initializing on demand: the inspectors and the tables window need values with no game
    // running to ask for them.  Application.isPlaying is true in a player, so only the Editor is excluded.
    bool Deferred => m_DeferInitialization && Application.isPlaying
        && !m_InitializationRequested && !m_Initialized;

    internal static bool InitializationSettled => s_Instance == null || s_Instance.m_Initialized || s_Instance.m_Preparing;

    Awaitable EnsureInitialized(bool mayStart)
    {
        // See InitializationSettled: waiting while the run prepares would deadlock, so resolve with the current state.
        if (m_Initialized || m_Preparing)
            return AwaitableUtility.Completed();
        // Awaitables are single-await, so every caller gets its own completion source.
        var waiter = new AwaitableCompletionSource();
        (m_InitWaiters ??= new List<AwaitableCompletionSource>()).Add(waiter);
        if (mayStart || !Deferred)
            StartInitialization();
        return waiter.Awaitable;
    }

    void StartInitialization()
    {
        // Flag set before the run: SelectedLocaleChanged handlers resolve strings, which lands back here.
        if (m_Initializing)
            return;
        m_Initializing = true;
        RunInitializeAsync();
    }

    async void RunInitializeAsync()
    {
        var generation = m_InitGeneration;
        var cts = m_InitCts = new CancellationTokenSource();
        try
        {
            m_Preparing = true;
            try
            {
                await DiscoverProviderLocalesAsync(cts.Token);
                // After discovery, so a selector preparing itself already sees every locale a provider contributed.
                await PrepareSelectorsAsync(cts.Token);
            }
            finally
            {
                // An abandoned run unwinding late must not clear the flag out from under the run that owns it now.
                if (generation == m_InitGeneration)
                    m_Preparing = false;
            }
            // A play-mode reset abandoned this run; its waiters are already completed and a new run owns the state.
            if (generation != m_InitGeneration)
                return;

            var selected = SelectStartupLocale(null, out var selectedBy);
            StartupSelector = selectedBy;
            if (selected != null)
                SetSelectedLocale(selected);

            m_Initialized = true;
            if (m_StartupSelectors != null)
            {
                foreach (var selector in m_StartupSelectors)
                    (selector as IStartupLocaleInitialize)?.PostInitialize(this);
            }
            InitializationCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            // async void: an escaping exception would be unhandled; resolvers proceed with the project locale.
            Debug.LogException(e);
        }
        finally
        {
            if (generation == m_InitGeneration)
            {
                m_Initialized = true;
                CompleteInitWaiters();
            }
            // A newer run may own m_InitCts by now; only clear it when it is still this run's.
            if (m_InitCts == cts)
                m_InitCts = null;
            cts.Dispose();
        }
    }

    async Awaitable PrepareSelectorsAsync(CancellationToken cancellationToken)
    {
        if (m_StartupSelectors == null)
            return;
        for (var i = 0; i < m_StartupSelectors.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (m_StartupSelectors[i] is not IStartupLocalePrepare prepare)
                continue;
            try
            {
                await prepare.PrepareAsync(this, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                // One selector failing to load must not cost the run its remaining selectors or the project locale.
                Debug.LogException(e);
            }
        }
    }

    // The chain the run evaluates, shared with EvaluateStartupLocale so both apply the same rules.
    Locale SelectStartupLocale(Func<IStartupLocaleSelector, bool> filter, out IStartupLocaleSelector selectedBy)
    {
        selectedBy = null;
        if (m_StartupSelectors != null)
        {
            for (var i = 0; i < m_StartupSelectors.Count; i++)
            {
                var selector = m_StartupSelectors[i];
                if (selector == null || (filter != null && !filter(selector)))
                    continue;
                var candidate = selector.GetStartupLocale(this);
                // A disabled locale is not offered at runtime, so skip it and let the next selector decide.
                if (candidate == null || !candidate.Enabled)
                    continue;
                selectedBy = selector;
                return candidate;
            }
        }
        return ProjectLocale;
    }
    async Awaitable DiscoverProviderLocalesAsync(CancellationToken cancellationToken)
    {
        var providers = m_ResourceDatabase?.AssetProvider?.Providers;
        if (providers == null)
            return;
        for (var i = 0; i < providers.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (providers[i] is ILocaleDiscovery discovery)
                await discovery.DiscoverLocalesAsync(this, cancellationToken);
        }
    }

    void CompleteInitWaiters()
    {
        var waiters = m_InitWaiters;
        m_InitWaiters = null;
        if (waiters == null)
            return;
        foreach (var waiter in waiters)
            waiter.SetResult();
    }

    void OnEnable()
    {
        if (s_Instance == null)
            s_Instance = this;
        if (string.IsNullOrEmpty(m_SelectedLocaleCode))
            m_SelectedLocaleCode = m_ProjectLocaleCode;
    }
}
