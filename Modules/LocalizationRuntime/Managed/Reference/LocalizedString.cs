// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.PersistentVariables;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// A serializable reference that resolves and formats a localized text value.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedString"/> extends <see cref="LocalizedEntry{TEntry}"/> to resolve an <see cref="IStringEntry"/> and
/// return its formatted text. Smart entries are formatted through Smart Strings, with placeholders such as <c>{score}</c>
/// resolved against the reference's <see cref="LocalVariables"/>, and the resolved value is passed through the locale's
/// post-processing hook before it is returned. Resolve the value with <see cref="GetLocalizedStringAsync"/>, or read it
/// synchronously with <see cref="GetLocalizedString"/> once the table is loaded. Subscribe to <see cref="StringChanged"/> to
/// receive the value immediately and again whenever the selected locale changes on <see cref="LocalizationSettings"/>.
/// </remarks>
/// <example>
/// <para>Resolve a localized string and keep a label updated as the locale changes.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringResolveExample.cs"/>
/// </example>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="ResourceDatabase"/>
/// <seealso cref="Unity.Localization.TableReference"/>
/// <seealso cref="Unity.Localization.TableEntryReference"/>
/// <seealso cref="LocalVariablesGroup"/>
/// <seealso cref="IVariable"/>
[Serializable]
public partial class LocalizedString : LocalizedEntry<IStringEntry>, IVariable
{
    /// <summary>
    /// Represents a method that receives a resolved localized string value.
    /// </summary>
    /// <remarks>Used by the <see cref="StringChanged"/> event to deliver the formatted value to subscribers.</remarks>
    /// <param name="value">The resolved and formatted localized string.</param>
    public delegate void ChangeHandler(string value);

    [SerializeField] LocalVariablesGroup m_LocalVariables = new();

    ChangeHandler m_Changed;
    Action<Locale> m_LocaleChanged;

    // Plain ints, incremented and decremented in a finally, so they are already balanced across a reload.
    [NoAutoStaticsCleanup] [ThreadStatic] static int s_ResolveDepth;
    [NoAutoStaticsCleanup] [ThreadStatic] static int s_ResolveBudget;

    const int k_MaxResolveDepth = 16;
    const int k_MaxNestedResolves = 256;

    /// <summary>
    /// The local variables that Smart String placeholders resolve against when this string is formatted.
    /// </summary>
    /// <remarks>
    /// Placeholders such as <c>{score}</c> look up values in this <see cref="LocalVariablesGroup"/> during formatting. A
    /// nested <see cref="LocalizedString"/> also reads the variables of the string that contains it (a chained scope).
    /// </remarks>
    public LocalVariablesGroup LocalVariables => m_LocalVariables;

    /// <summary>
    /// Occurs when the resolved string value changes, and once immediately when a handler subscribes.
    /// </summary>
    /// <remarks>
    /// Adding a handler invokes it right away with the current value, then again whenever the selected locale changes or
    /// <see cref="RefreshString"/> runs. Because resolution is asynchronous, the first call arrives after the value has
    /// loaded. Removing the last handler stops the reference from listening for locale changes.
    /// </remarks>
    /// <example>
    /// <para>Update a UI label whenever the localized value changes.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringChangedExample.cs"/>
    /// </example>
    /// <seealso cref="RefreshString"/>
    /// <seealso cref="GetLocalizedStringAsync"/>
    public event ChangeHandler StringChanged
    {
        add
        {
            RegisterLocaleChanged();
            m_Changed += value;
            PushTo(value);
        }
        remove
        {
            m_Changed -= value;
            if (m_Changed == null)
                UnregisterLocaleChanged();
        }
    }

    /// <summary>
    /// Resolves the localized string asynchronously, formatting it with the given arguments.
    /// </summary>
    /// <remarks>
    /// Loads the table through the <see cref="ResourceDatabase"/> provider chain if needed, formats Smart String placeholders
    /// against the supplied arguments and <see cref="LocalVariables"/>, and returns an empty string when the reference is
    /// empty. When <see cref="LocalizationSettings.PreferredLoading"/> is <see cref="LoadingPreference.Synchronous"/>, it
    /// resolves synchronously when the value is available and otherwise falls back to this asynchronous load.
    /// </remarks>
    /// <param name="args">Optional arguments that Smart String placeholders format against.</param>
    /// <returns>An awaitable that produces the formatted localized string, or an empty string when the reference is empty.</returns>
    /// <example>
    /// <para>Resolve a string and format it with an argument.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringFormatAsyncExample.cs"/>
    /// </example>
    public Awaitable<string> GetLocalizedStringAsync(params object[] args)
    {
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return AwaitableUtility.FromResult(string.Empty);
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var value = GetLocalizedString(args);
            if (!string.IsNullOrEmpty(value))
                return AwaitableUtility.FromResult(value);
        }
        return FormatAsync(database, args);
    }

    async Awaitable<string> FormatAsync(ResourceDatabase database, object[] args)
    {
        // Initialization selects the startup locale, so settle it before capturing the resolving locale.
        if (!LocalizationSettings.InitializationSettled)
            await LocalizationSettings.EnsureInitializedForUse();
        var locale = ResolvingLocale();
        var entry = await GetEntryAsync(locale, default);
        return database.FormatEntry(entry, locale, args, Scope(null));
    }

    /// <summary>
    /// Resolves the localized string synchronously when the table is cached or a synchronous provider can supply it.
    /// </summary>
    /// <remarks>
    /// Formats Smart String placeholders against the supplied arguments and <see cref="LocalVariables"/>. The table
    /// resolves from the cache, or through a provider that supports synchronous loading; the result is an empty string
    /// when the reference is empty or the table can only be loaded asynchronously. Use
    /// <see cref="GetLocalizedStringAsync"/> to load the table asynchronously instead.
    /// </remarks>
    /// <param name="args">Optional arguments that Smart String placeholders format against.</param>
    /// <returns>The formatted localized string, or an empty string when it is not available synchronously.</returns>
    /// <example>
    /// <para>Read an already-loaded localized string.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringSyncExample.cs"/>
    /// </example>
    public string GetLocalizedString(params object[] args) => ResolveWithScope(null, args);

    /// <summary>
    /// Resolves this string so it can be read as a Smart String placeholder in another string.
    /// </summary>
    /// <remarks>
    /// Add a <see cref="LocalizedString"/> to another string's <see cref="LocalVariables"/> to embed one entry inside
    /// another. The nested string reads its own variables layered over the containing string's (a chained scope). Two
    /// entries that reference each other resolve as empty and log a warning rather than recursing without end.
    /// </remarks>
    /// <param name="selector">Describes the placeholder being resolved.</param>
    /// <returns>The resolved text, or an empty string when the reference is unset or the nesting limit is reached.</returns>
    /// <example>
    /// <para>Embed one localized string inside another as a local variable.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringNestedExample.cs"/>
    /// </example>
    /// <seealso cref="LocalVariables"/>
    /// <seealso cref="LocalVariablesGroup"/>
    public object GetSourceValue(ISelectorInfo selector)
    {
        if (IsEmpty)
            return string.Empty;
        if (s_ResolveDepth == 0)
            s_ResolveBudget = k_MaxNestedResolves;
        if (s_ResolveDepth >= k_MaxResolveDepth || s_ResolveBudget <= 0)
        {
            Debug.LogWarning($"Nested localized strings went past the resolve limit, which usually means two entries reference each other. Resolving '{TableEntryReference}' as an empty string to break the cycle.");
            return string.Empty;
        }
        var parentScope = selector?.FormatDetails?.OriginalFormat?.AdditionalData?.LocalVariables;
        s_ResolveDepth++;
        s_ResolveBudget--;
        try
        {
            return ResolveWithScope(parentScope);
        }
        finally
        {
            s_ResolveDepth--;
        }
    }

    internal string ResolveWithScope(IVariableGroup parentScope, params object[] args)
    {
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return string.Empty;
        var entry = GetEntry();
        return database.FormatEntry(entry, ResolvingLocale(), args, Scope(parentScope));
    }

    IVariableGroup Scope(IVariableGroup parentScope)
    {
        var own = m_LocalVariables != null && m_LocalVariables.Count > 0 ? m_LocalVariables : null;
        if (own == null)
            return parentScope;
        return parentScope == null ? own : new ChainedVariableGroup(own, parentScope);
    }

    /// <summary>
    /// Re-resolves the value and pushes the result to subscribers.
    /// </summary>
    /// <remarks>
    /// Resolves the string again and invokes <see cref="StringChanged"/> with the new value. Does nothing when no handler is
    /// subscribed. It runs automatically when the reference changes or the selected locale changes, so call it manually only
    /// when an external input, such as a value in <see cref="LocalVariables"/>, changes.
    /// </remarks>
    /// <example>
    /// <para>Refresh after changing a local variable that the string formats against.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedStringRefreshExample.cs"/>
    /// </example>
    public void RefreshString()
    {
        if (m_Changed == null)
            return;
        var version = BeginRefresh();
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var value = GetLocalizedString();
            if (!string.IsNullOrEmpty(value))
            {
                m_Changed?.Invoke(value);
                return;
            }
        }
        RefreshStringAsync(version);
    }

    async void RefreshStringAsync(int version)
    {
        try
        {
            var value = await GetLocalizedStringAsync();
            if (!IsCurrentRefresh(version))
                return;
            m_Changed?.Invoke(value);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            // async void: an escaping exception would be unhandled and can bring down the player.
            Debug.LogException(e);
        }
    }

    void PushTo(ChangeHandler handler)
    {
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var value = GetLocalizedString();
            if (!string.IsNullOrEmpty(value))
            {
                handler?.Invoke(value);
                return;
            }
        }
        PushToAsync(handler);
    }

    async void PushToAsync(ChangeHandler handler)
    {
        var version = CurrentRefresh;
        try
        {
            var value = await GetLocalizedStringAsync();
            // A refresh started since; it pushes the newer value to this handler too.
            if (!IsCurrentRefresh(version))
                return;
            // The handler may have unsubscribed while the load was in flight, so deliver only if it is still live.
            if (IsSubscribed(handler))
                handler(value);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <inheritdoc/>
    protected override void ForceUpdate()
    {
        base.ForceUpdate();
        RefreshString();
    }

    bool IsSubscribed(ChangeHandler handler)
    {
        var current = m_Changed;
        if (current == null || handler == null)
            return false;
        if (current == handler)
            return true;
        // Compare target by target, so a handler subscribed as an already-combined delegate still matches.
        var live = current.GetInvocationList();
        var wanted = handler.GetInvocationList();
        for (var i = 0; i < wanted.Length; i++)
        {
            if (Array.IndexOf(live, wanted[i]) < 0)
                return false;
        }
        return true;
    }

    void RegisterLocaleChanged()
    {
        m_LocaleChanged ??= _ => RefreshString();
        // Remove before adding so this subscribes exactly once whatever state the static event is in.
        LocalizationSettings.SelectedLocaleChanged -= m_LocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += m_LocaleChanged;
    }

    void UnregisterLocaleChanged()
    {
        if (m_LocaleChanged == null)
            return;
        LocalizationSettings.SelectedLocaleChanged -= m_LocaleChanged;
        m_LocaleChanged = null;
    }
}
