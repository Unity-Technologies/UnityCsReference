// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// The serializable base for a reference into a localization table, pairing a table collection with an optional locale override.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedReference"/> identifies the <see cref="Unity.Localization.TableReference"/> to resolve against and,
/// optionally, the <see cref="LocaleOverride"/> to resolve in. When no override is set, the reference resolves in the locale
/// selected on <see cref="LocalizationSettings"/>. Concrete subclasses resolve either a whole table
/// (<see cref="LocalizedTable"/>) or a single entry within one (<see cref="LocalizedEntry{TEntry}"/>, along with the
/// <see cref="LocalizedString"/> and <see cref="LocalizedAsset{TObject}"/> types built on it). Resolution goes through the
/// <see cref="ResourceDatabase"/> provider chain, so lookups are asynchronous by default; set
/// <see cref="LocalizationSettings.PreferredLoading"/> to <see cref="LoadingPreference.Synchronous"/> to resolve
/// synchronously when the value is available.
/// Store a reference in a serialized field to expose it in the Inspector, then read the localized value at runtime.
/// </remarks>
/// <example>
/// <para>Configure a reference through the derived <see cref="LocalizedString"/> type and choose the locale it resolves in.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedReferenceConfigureExample.cs"/>
/// </example>
/// <seealso cref="LocalizedString"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="LocalizedTable"/>
/// <seealso cref="ResourceDatabase"/>
/// <seealso cref="Unity.Localization.TableReference"/>
[Serializable]
public abstract partial class LocalizedReference
{
    [SerializeField] TableReference m_TableReference;
    [SerializeField] LocaleIdentifier m_LocaleOverride;
    [SerializeField] bool m_EnableFallback = true;

    // Tags each refresh so a slow, out-of-date load can't overwrite a newer value.
    int m_RefreshVersion;

    /// <summary>
    /// Starts a new refresh and returns its version number.
    /// </summary>
    /// <remarks>
    /// Call this at the start of a refresh that loads a new value. It bumps the counter, which supersedes any
    /// refresh already waiting on a load: that refresh's version no longer matches, so it discards its result
    /// when it finishes. The refresh methods are async, so several loads can be in flight at once (for example
    /// when the locale changes twice in quick succession) and can finish in any order; the version keeps the
    /// newest one authoritative. Keep the returned number and pass it to <see cref="IsCurrentRefresh"/> once the
    /// load completes.
    /// </remarks>
    /// <returns>The version number assigned to this refresh.</returns>
    internal int BeginRefresh() => ++m_RefreshVersion;

    /// <summary>
    /// Reports whether a refresh is still the most recent one.
    /// </summary>
    /// <remarks>
    /// Call this after a load finishes, with the number from <see cref="BeginRefresh"/> or
    /// <see cref="CurrentRefresh"/>. It returns <see langword="false"/> when a newer refresh has started in the
    /// meantime, which is the signal to drop the loaded value instead of publishing a stale one.
    /// </remarks>
    /// <param name="version">The version number captured when the refresh started.</param>
    /// <returns><see langword="true"/> when no newer refresh has started since <paramref name="version"/>; otherwise <see langword="false"/>.</returns>
    internal bool IsCurrentRefresh(int version) => version == m_RefreshVersion;

    /// <summary>
    /// The version of the current refresh, read without starting a new one.
    /// </summary>
    /// <remarks>
    /// Use this instead of <see cref="BeginRefresh"/> to deliver the value that is already current rather than
    /// trigger a new load, such as pushing the current value to a handler that just subscribed. Capture this
    /// number before the load, then check it with <see cref="IsCurrentRefresh"/> afterwards. If a real refresh
    /// started in between, the check fails and the push is skipped, because that refresh delivers the newer
    /// value to every handler anyway.
    /// </remarks>
    internal int CurrentRefresh => m_RefreshVersion;

    /// <summary>
    /// The localization table collection that this reference resolves against.
    /// </summary>
    /// <remarks>
    /// Assigning a new value re-resolves the reference and notifies any live listeners. A value can be set from a table
    /// collection name or its GUID, because <see cref="Unity.Localization.TableReference"/> converts from both.
    /// </remarks>
    public TableReference TableReference
    {
        get => m_TableReference;
        set { m_TableReference = value; ForceUpdate(); }
    }

    /// <summary>
    /// The locale used to resolve this reference instead of the locale selected in the project settings.
    /// </summary>
    /// <remarks>
    /// Set this to force a specific locale regardless of the current selection. When the identifier is undefined (the default),
    /// the reference resolves in the selected locale. Use <see cref="HasLocaleOverride"/> to check whether an override is set.
    /// Assigning a new value re-resolves the reference and notifies any live listeners.
    /// </remarks>
    public LocaleIdentifier LocaleOverride
    {
        get => m_LocaleOverride;
        set { m_LocaleOverride = value; ForceUpdate(); }
    }

    /// <summary>
    /// Whether to walk the locale fallback chain when the resolving locale has no value for this reference.
    /// </summary>
    /// <remarks>
    /// When this is <c>true</c> and the resolving locale has no value, resolution continues through that locale's fallback
    /// chain. When it is <c>false</c>, a missing value resolves to empty. Assigning a new value re-resolves the reference and
    /// notifies any live listeners.
    /// </remarks>
    public bool EnableFallback
    {
        get => m_EnableFallback;
        set { m_EnableFallback = value; ForceUpdate(); }
    }

    /// <summary>
    /// Whether a locale override is set on this reference.
    /// </summary>
    /// <remarks>Returns <c>true</c> when <see cref="LocaleOverride"/> holds a defined locale identifier.</remarks>
    public bool HasLocaleOverride => !string.IsNullOrEmpty(m_LocaleOverride.Code);

    /// <summary>
    /// Whether this reference is unset and resolves to no value.
    /// </summary>
    /// <remarks>
    /// The base implementation returns <c>true</c> when no <see cref="Unity.Localization.TableReference"/> is assigned.
    /// Subclasses extend this to also require a valid entry reference.
    /// </remarks>
    public virtual bool IsEmpty => m_TableReference.IsEmpty;

    /// <summary>
    /// Resolves the override locale from the project settings for subclasses to pass to the resource database.
    /// </summary>
    /// <remarks>
    /// When <see cref="HasLocaleOverride"/> is <c>true</c> and a <see cref="LocalizationSettings"/> instance exists, this looks
    /// up the <see cref="LocaleOverride"/> identifier with <see cref="LocalizationSettings.GetLocale(LocaleIdentifier)"/>.
    /// Otherwise it returns null so the resolver falls back to the selected locale.
    /// </remarks>
    /// <returns>The resolved override locale, or null when no override applies.</returns>
    protected Locale ResolveOverrideLocale()
        => HasLocaleOverride && LocalizationSettings.Instance != null ? LocalizationSettings.Instance.GetLocale(m_LocaleOverride) : null;

    private protected Locale ResolvingLocale() => ResolveOverrideLocale() ?? LocalizationSettings.SelectedLocale;

    /// <summary>
    /// Called when the reference changes so subclasses can refresh any live listeners.
    /// </summary>
    /// <remarks>
    /// The base implementation does nothing. Subclasses such as <see cref="LocalizedString"/> and
    /// <see cref="LocalizedAsset{TObject}"/> override this to re-resolve the value and push it to their change events. It runs
    /// whenever a property setter on the reference assigns a new value.
    /// </remarks>
    protected virtual void ForceUpdate() { }
}
