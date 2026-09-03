// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// A serializable reference to an entire localization table resolved for a locale.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedTable"/> extends <see cref="LocalizedReference"/> to resolve a whole <see cref="ResourceTable"/>
/// rather than a single entry. It resolves the table for the selected locale unless
/// <see cref="LocalizedReference.LocaleOverride"/> is set. Load the table with <see cref="GetTableAsync"/>, or read it
/// synchronously with <see cref="GetTable"/> once it is loaded. Use this to access many entries at once instead of one
/// <see cref="LocalizedEntry{TEntry}"/> per entry.
/// </remarks>
/// <example>
/// <para>Resolve a whole table for the selected locale.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedTableResolveExample.cs"/>
/// </example>
/// <seealso cref="ResourceDatabase"/>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="Unity.Localization.TableReference"/>
[Serializable]
public class LocalizedTable : LocalizedReference
{
    /// <summary>
    /// Resolves the table asynchronously for the reference's locale.
    /// </summary>
    /// <remarks>
    /// Loads the table through the <see cref="ResourceDatabase"/> provider chain if needed. The result is null when the
    /// reference is empty, no resource database is configured, or the table cannot be found. When
    /// <see cref="LocalizationSettings.PreferredLoading"/> is <see cref="LoadingPreference.Synchronous"/> and the table is
    /// available synchronously, the table resolves without an asynchronous load.
    /// </remarks>
    /// <param name="cancellationToken">A token that cancels the asynchronous load. The default token never cancels.</param>
    /// <returns>An awaitable that produces the resolved table, or null when it cannot be resolved.</returns>
    /// <example>
    /// <para>Load a table asynchronously.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedTableGetTableAsyncExample.cs"/>
    /// </example>
    public Awaitable<ResourceTable> GetTableAsync(CancellationToken cancellationToken = default)
    {
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return AwaitableUtility.FromResult<ResourceTable>(null);
        if (LocalizationSettings.PreferredLoading == LoadingPreference.Synchronous)
        {
            var table = GetTable();
            if (table != null)
                return AwaitableUtility.FromResult(table);
        }
        return database.GetTableAsync(TableReference, ResolveOverrideLocale(), cancellationToken);
    }

    /// <summary>
    /// Returns the table synchronously if it is already loaded.
    /// </summary>
    /// <remarks>
    /// Does not trigger a load. The result is null when the table is not loaded yet, the reference is empty, or no resource
    /// database is configured. Use <see cref="GetTableAsync"/> to load the table on demand instead.
    /// </remarks>
    /// <returns>The resolved table, or null when it is not available synchronously.</returns>
    /// <example>
    /// <para>Read an already-loaded table.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedTableGetTableExample.cs"/>
    /// </example>
    public ResourceTable GetTable()
    {
        var database = LocalizationSettings.ResourceDatabase;
        return database != null && !IsEmpty ? database.GetTable(TableReference, ResolveOverrideLocale()) : null;
    }
}
