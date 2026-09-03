// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Localization.Editor;

/// <summary>
/// Provides editor access to the active localization settings for the project.
/// </summary>
/// <remarks>
/// The active settings are stored as an <see cref="EditorBuildSettings"/> config object and included in
/// player builds. Use <see cref="ActiveSettings"/> to read or assign them, and <see cref="CollectionsChanged"/>
/// to react to <see cref="ResourceTableCollection"/> assets being created, imported, or deleted.
/// </remarks>
public static partial class LocalizationEditorSettings
{
    /// <summary>
    /// Occurs when the set of table collections in the project changes.
    /// </summary>
    /// <remarks>
    /// Raised after a <see cref="ResourceTableCollection"/> is created, imported, or deleted so editor UI can refresh.
    /// </remarks>
    [AutoStaticsCleanup] // holds subscriber delegates
    public static event Action CollectionsChanged;

    /// <summary>
    /// Raises the <see cref="CollectionsChanged"/> event.
    /// </summary>
    /// <remarks>
    /// Call this after creating a <see cref="ResourceTableCollection"/> asset in code so open windows update.
    /// </remarks>
    internal static void RaiseCollectionsChanged() => CollectionsChanged?.Invoke();

    /// <summary>
    /// Adds a table for a locale to a collection, or returns the collection's existing table for that locale.
    /// </summary>
    /// <remarks>
    /// Creates the per-locale <see cref="ResourceTable"/> as a separate asset in the collection's folder, registers the
    /// collection with its content source, and saves. When the collection already has a table for the locale, that
    /// table is returned unchanged. The locale is normally one of the project's
    /// <see cref="LocalizationSettings.AvailableLocales"/>.
    /// </remarks>
    /// <param name="collection">The collection to add the table to.</param>
    /// <param name="locale">The locale the table targets.</param>
    /// <returns>
    /// The new or existing <see cref="ResourceTable"/>, or <see langword="null"/> when the collection or locale is
    /// <see langword="null"/>, or the collection has no shared data.
    /// </returns>
    /// <seealso cref="CreateStringEntry"/>
    /// <seealso cref="ResourceTableCollection"/>
    public static ResourceTable AddTable(ResourceTableCollection collection, Locale locale)
        => LocalizationTableAuthoring.EnsureLocaleTable(collection, locale);

    /// <summary>
    /// Adds a string key to a collection across every one of its locale tables.
    /// </summary>
    /// <remarks>
    /// Registers the key in the collection's <see cref="SharedTableData"/> and adds an empty <see cref="StringEntry"/>
    /// for it to each table, then saves. Checking the key text for empty or duplicate names is the caller's job; an
    /// empty key adds nothing and returns zero.
    /// </remarks>
    /// <param name="collection">The collection to add the key to.</param>
    /// <param name="key">The key text.</param>
    /// <returns>The new key's stable id, or zero when the collection has no shared data or the key is empty.</returns>
    /// <seealso cref="AddTable"/>
    /// <seealso cref="ResourceTableCollection"/>
    public static long CreateStringEntry(ResourceTableCollection collection, string key)
        => LocalizationTableAuthoring.CreateStringEntry(collection, key);

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        RegisterActiveSettings();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += FileTablePlaymodeGenerator.ClearAndRelease;
        // A collection or table added since the last scan is not in the edit mode source map yet.
        CollectionsChanged += OnCollectionsChanged;

        // The reload that enters Play Mode runs before isPlaying is set, and it dropped the providers' editor state.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            FileTablePlaymodeGenerator.PrepareForPlayMode();
        else
            FileTablePlaymodeGenerator.PrepareForEditMode();
    }

    [AutoStaticsCleanup] // a queued delayCall does not survive the reload that would clear this
    static bool s_EditModeRefreshQueued;

    static void OnCollectionsChanged()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || s_EditModeRefreshQueued)
            return;
        // Deferred so the collection cache another listener drops on this same event is rebuilt first, and so a burst
        // of imports refreshes once.
        s_EditModeRefreshQueued = true;
        EditorApplication.delayCall += RefreshEditModeTables;
    }

    static void RefreshEditModeTables()
    {
        s_EditModeRefreshQueued = false;
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            FileTablePlaymodeGenerator.PrepareForEditMode();
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredEditMode)
            return;
        LocalizationSettings.ResetStaticsForPlayMode();
        VariantResolver.ResetStatics();
        Providers.FileTables.TableDataConverter.ResetStatics();
        RegisterActiveSettings();

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            FileTablePlaymodeGenerator.PrepareForPlayMode();
        }
        else
        {
            FileTablePlaymodeGenerator.ClearAndRelease();
            FileTablePlaymodeGenerator.PrepareForEditMode();
        }
    }

    static void RegisterActiveSettings()
    {
        LocalizationSettings.Instance = ActiveSettings;
    }

    /// <summary>
    /// Gets or sets the active localization settings for the project.
    /// </summary>
    /// <remarks>
    /// The value is persisted as an <see cref="EditorBuildSettings"/> config object and included in player builds.
    /// Setting it to <see langword="null"/> removes the config object.
    /// </remarks>
    internal static LocalizationSettings ActiveSettings
    {
        get
        {
            EditorBuildSettings.TryGetConfigObject(LocalizationSettings.ConfigName, out LocalizationSettings settings);
            return settings;
        }
        set
        {
            if (value == null)
                EditorBuildSettings.RemoveConfigObject(LocalizationSettings.ConfigName);
            else
                EditorBuildSettings.AddConfigObject(LocalizationSettings.ConfigName, value, true);
            LocalizationSettings.Instance = value;
        }
    }
}
