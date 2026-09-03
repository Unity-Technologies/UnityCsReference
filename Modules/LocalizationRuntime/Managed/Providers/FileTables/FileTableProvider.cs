// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Unity.Localization.Providers.FileTables;

/// <summary>
/// Selects what a file table provider serves in the editor when entering Play Mode.
/// </summary>
/// <seealso cref="FileTableProvider"/>
public enum PlayModeSource
{
    /// <summary>
    /// Serve the live authored table assets directly, without generating files. This is the fast default for entering Play Mode.
    /// </summary>
    SourceAssets,

    /// <summary>
    /// Generate the data files and read them back, exercising the real file path.
    /// </summary>
    GeneratedFiles
}

/// <summary>
/// An asset provider that serves a table by reading a data file and reconstructing the table in memory.
/// </summary>
/// <remarks>
/// A table whose entries all round-trip as plain data never ships its authored asset; a table holding object
/// references (entries that are not <see cref="IFileDataEntry"/>) keeps its asset in the build, and those
/// entries are grafted onto the file-built table at load. One file is
/// written per table per locale, named <c>{collectionName}_{localeCode}.{extension}</c>. Subclass and return an
/// <see cref="ITableFileReader"/> to add a format; JSON is the default (<see cref="JsonResourceProvider"/>). In a
/// player build the files live under <c>StreamingAssets/{SubPath}</c>. In the editor a base-path override points
/// the provider at the Library folder, so nothing is written into the project, and in the default Play Mode mode
/// the provider serves the live table assets and reads no files at all.
/// </remarks>
/// <seealso cref="JsonResourceProvider"/>
/// <seealso cref="ITableFileReader"/>
/// <seealso cref="IAssetProvider"/>
[Serializable]
public abstract class FileTableProvider : IAsyncAssetProvider, ILocaleDiscovery
{
    /// <summary>
    /// A collection this provider serves, by guid and name so either address form resolves.
    /// </summary>
    // A class, not a struct: serializable structs holding reference fields crash CoreCLR marshalling (UUM-145517).
    [Serializable]
    public sealed class CollectionRef
    {
        /// <summary>
        /// The collection guid, which is also the file name stem.
        /// </summary>
        public string Guid;

        /// <summary>
        /// The collection name, an alternate address form.
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// A table asset kept in the build because some of its entries cannot ship as file data.
    /// </summary>
    /// <remarks>
    /// Internal: only the editor generator writes these (through <see cref="SetBakedTables"/>) and only the
    /// provider reads them back, so there is nothing an external caller can do with one.
    /// </remarks>
    // A class, not a struct: serializable structs holding reference fields crash CoreCLR marshalling (UUM-145517).
    [Serializable]
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal sealed class BakedTable
    {
        /// <summary>
        /// The canonical address the table resolves under.
        /// </summary>
        /// <remarks>
        /// The address form is <c>{collectionGuid}_{localeCode}</c>.
        /// </remarks>
        public string Address;

        /// <summary>
        /// The table asset kept in the build.
        /// </summary>
        /// <remarks>
        /// Held through <see cref="UnityEngine.LazyLoadReference{T}"/>, which defers loading until the asset is
        /// accessed in the editor. In a player it behaves as a normal reference that ships and loads with the object
        /// that holds it, so the deferral is an editor-only benefit.
        /// </remarks>
        public LazyLoadReference<ResourceTable> Table;
    }

    // Not hand-edited: the editor fills this in, and a guid/name pair is not a useful thing to type.
    [SerializeField, HideInInspector] List<CollectionRef> m_Collections = new();
    // The records the editor registered, so a rebuild can drop them without touching records added through AddCollection.
    [SerializeField, HideInInspector] List<string> m_OwnedCollections = new();
    // Written only by the generator, so a hand edit would be overwritten on the next generate.
    [SerializeField, HideInInspector] List<BakedTable> m_BakedTables = new();
    [SerializeField, Tooltip("Subfolder of StreamingAssets the generated data files are written to in a player build.")]
    string m_SubPath = "LocalizationTables";
    [SerializeField, Tooltip("What this source serves when entering Play Mode: the live table assets, or the generated data files.")]
    PlayModeSource m_PlayModeSource = PlayModeSource.SourceAssets;


    Dictionary<string, ResourceTable> m_SourceTables;
    HashSet<string> m_WarnedAddresses;
    HashSet<ResourceTable> m_Constructed;
    string m_EditorBasePathOverride;

    /// <summary>
    /// Reads a table snapshot from a file in this provider's format.
    /// </summary>
    protected abstract ITableFileReader Reader { get; }

    /// <summary>
    /// The subfolder within StreamingAssets the files are placed in, customizable per provider.
    /// </summary>
    public string SubPath => m_SubPath;

    /// <summary>
    /// The file extension the format uses, without the leading dot.
    /// </summary>
    public string FileExtension => Reader.FileExtension;

    /// <summary>
    /// What this provider serves in the editor when entering Play Mode.
    /// </summary>
    public PlayModeSource PlayMode => m_PlayModeSource;

    /// <summary>
    /// The collections this provider serves.
    /// </summary>
    public IReadOnlyList<CollectionRef> Collections => m_Collections;

    /// <summary>
    /// Records a collection so its addresses resolve at runtime.
    /// </summary>
    /// <remarks>
    /// Updates the name if the guid is already known.
    /// </remarks>
    /// <param name="guid">The collection guid.</param>
    /// <param name="name">The collection name.</param>
    public void AddCollection(string guid, string name)
    {
        Record(guid, name);
        // A record set through this method is no longer editor-owned, so a rebuild leaves it alone.
        m_OwnedCollections.Remove(guid);
    }

    // Editor-owned, so a rebuild can drop it without disturbing records added through AddCollection.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void AddOwnedCollection(string guid, string name)
    {
        Record(guid, name);
        if (!string.IsNullOrEmpty(guid) && !m_OwnedCollections.Contains(guid))
            m_OwnedCollections.Add(guid);
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void RemoveOwnedCollection(string guid)
    {
        if (string.IsNullOrEmpty(guid) || !m_OwnedCollections.Remove(guid))
            return;
        RemoveRecord(guid);
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void ClearOwnedCollections()
    {
        for (var i = 0; i < m_OwnedCollections.Count; i++)
            RemoveRecord(m_OwnedCollections[i]);
        m_OwnedCollections.Clear();
    }

    void Record(string guid, string name)
    {
        if (string.IsNullOrEmpty(guid))
            return;
        for (var i = 0; i < m_Collections.Count; i++)
        {
            // A record deserializes as null when the stored data does not round-trip, so never assume one is there.
            if (m_Collections[i] != null && string.Equals(m_Collections[i].Guid, guid, StringComparison.Ordinal))
            {
                m_Collections[i] = new CollectionRef { Guid = guid, Name = name };
                return;
            }
        }
        m_Collections.Add(new CollectionRef { Guid = guid, Name = name });
    }

    bool RemoveRecord(string guid)
    {
        for (var i = 0; i < m_Collections.Count; i++)
        {
            if (m_Collections[i] != null && string.Equals(m_Collections[i].Guid, guid, StringComparison.Ordinal))
            {
                m_Collections.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Removes a collection so this provider no longer serves its addresses.
    /// </summary>
    /// <remarks>
    /// Call this when a collection is deleted or moved to another provider. A record left behind keeps claiming the
    /// collection's name as an address prefix, so data files left in the output folder still resolve to it and a
    /// surviving collection whose name starts with the same text can be parsed against the stale prefix. An empty or
    /// null guid is treated as absent. This does not delete any generated file; regenerating prunes those.
    /// </remarks>
    /// <param name="guid">The guid of the collection to remove.</param>
    /// <returns><see langword="true"/> if a collection was removed; otherwise, <see langword="false"/>.</returns>
    /// <example>
    /// <para>Stop serving a collection.</para>
    /// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/FileTableProviderRemoveCollectionExample.cs"/>
    /// </example>
    /// <seealso cref="AddCollection"/>
    /// <seealso cref="ClearCollections"/>
    public bool RemoveCollection(string guid)
    {
        if (string.IsNullOrEmpty(guid) || !RemoveRecord(guid))
            return false;
        m_OwnedCollections.Remove(guid);
        return true;
    }

    /// <summary>
    /// Removes every collection this provider serves.
    /// </summary>
    /// <remarks>
    /// The editor calls this before rebuilding the records from the collections that still exist, so a deleted or
    /// reassigned collection does not linger. Leaves the baked tables and the configured sub path alone.
    /// </remarks>
    /// <example>
    /// <para>Drop every record before rebuilding them.</para>
    /// <code source="../../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Providers/FileTableProviderClearCollectionsExample.cs"/>
    /// </example>
    /// <seealso cref="AddCollection"/>
    /// <seealso cref="RemoveCollection"/>
    public void ClearCollections()
    {
        m_Collections.Clear();
        m_OwnedCollections.Clear();
    }

    /// <summary>
    /// The tables kept in the build because some of their entries cannot ship as file data.
    /// </summary>
    internal IReadOnlyList<BakedTable> BakedTables
    {
        [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
        get => m_BakedTables;
    }

    // The editor populates these at generation time for tables holding entries that cannot round-trip as file data.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void SetBakedTables(List<BakedTable> tables)
    {
        m_BakedTables.Clear();
        if (tables != null)
            m_BakedTables.AddRange(tables);
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void SetSourceTables(Dictionary<string, ResourceTable> tables) => m_SourceTables = tables;

    // Reads files from a directory instead of StreamingAssets; the directory is the Library folder in Play Mode.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void SetEditorBasePathOverride(string directory) => m_EditorBasePathOverride = directory;

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void ClearEditorState()
    {
        m_SourceTables = null;
        m_EditorBasePathOverride = null;
        m_WarnedAddresses = null;
    }

    string BasePath => Application.isEditor && !string.IsNullOrEmpty(m_EditorBasePathOverride)
        ? m_EditorBasePathOverride
        : Path.Combine(Application.streamingAssetsPath, m_SubPath);

    /// <inheritdoc/>
    public async Awaitable<T> LoadAssetAsync<T>(AssetKey key, CancellationToken cancellationToken) where T : Object
    {
        cancellationToken.ThrowIfCancellationRequested();
        var table = await ResolveTableAsync(key, cancellationToken);
        if (table is T typed)
            return typed;
        // Built (and tracked) but not the requested type: release it so it is not leaked in m_Constructed.
        if (table != null)
            Release(table);
        return null;
    }

    /// <inheritdoc/>
    public async Awaitable<Object> LoadAssetAsync(AssetKey key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ResolveTableAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Scans the table folder for per-locale files named "{collectionName}_{localeCode}" and registers any locale
    /// not already available, so languages added to a built player become selectable. Discovered locales fall back
    /// to the project locale. Skipped on platforms without direct file access to StreamingAssets, such as Android and WebGL.
    /// </remarks>
    public Awaitable DiscoverLocalesAsync(LocalizationSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (settings != null)
            DiscoverLocales(settings);
        return AwaitableUtility.Completed();
    }

    void DiscoverLocales(LocalizationSettings settings)
    {
        var basePath = BasePath;
        try
        {
            if (IsUrl(basePath) || !Directory.Exists(basePath))
                return;
            // Sanitizing allocates per call; compute each collection's file-name prefix once, not per file.
            var prefixes = new List<string>(m_Collections.Count);
            for (var i = 0; i < m_Collections.Count; i++)
            {
                var record = m_Collections[i];
                if (record != null && !string.IsNullOrEmpty(record.Name))
                    prefixes.Add(Sanitize(record.Name));
            }
            if (prefixes.Count == 0)
                return;
            // Longest prefix first, so "My" does not parse "My_Table_en" as the locale "Table_en".
            prefixes.Sort((a, b) => b.Length.CompareTo(a.Length));
            var projectLocale = settings.ProjectLocale;
            foreach (var file in Directory.EnumerateFiles(basePath, "*." + FileExtension))
            {
                if (!TryParseFileName(Path.GetFileNameWithoutExtension(file), prefixes, out var localeCode))
                    continue;
                if (settings.GetLocale(localeCode) != null)
                    continue;
                var locale = new Locale(localeCode);
                // Fall back to the project locale so a partial translation shows base-language text instead of empty values.
                if (projectLocale != null && !string.Equals(projectLocale.Code, localeCode, StringComparison.OrdinalIgnoreCase))
                    locale.FallbackCode = projectLocale.Code;
                settings.AddLocale(locale);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Locale discovery failed for '{basePath}': {e.Message}");
        }
    }

    // The file name is "{collectionName}_{localeCode}" with the collection name sanitized for the file system.
    static bool TryParseFileName(string fileName, List<string> sanitizedNames, out string localeCode)
    {
        for (var i = 0; i < sanitizedNames.Count; i++)
        {
            if (TryStripPrefix(fileName, sanitizedNames[i], out localeCode))
                return true;
        }
        localeCode = null;
        return false;
    }

    /// <inheritdoc/>
    public void Release(Object asset)
    {
        // Only destroy tables this provider built from a file; live source assets are owned elsewhere.
        if (asset is not ResourceTable table || m_Constructed == null || !m_Constructed.Remove(table))
            return;
        var shared = table.SharedData;
        DestroyObject(table);
        if (shared != null)
            DestroyObject(shared);
    }

    async Awaitable<ResourceTable> ResolveTableAsync(AssetKey key, CancellationToken cancellationToken)
    {
        if (key.Type != typeof(ResourceTable) || string.IsNullOrEmpty(key.Address))
            return null;

        if (m_SourceTables != null && m_SourceTables.TryGetValue(key.Address, out var source) && source != null)
            return source;

        if (!TryParseAddress(key.Address, out var collectionGuid, out var collectionName, out var localeCode))
            return null;

        var path = Path.Combine(BasePath, FileName(collectionName, localeCode, FileExtension));
        try
        {
            ResourceTableData data;
            if (IsUrl(path))
            {
                var bytes = await ReadRemote(path, cancellationToken);
                if (bytes == null)
                    return MissingTable(collectionGuid, collectionName, localeCode, path);
                using var stream = new MemoryStream(bytes);
                data = Reader.Read(stream);
            }
            else
            {
                if (!File.Exists(path))
                    return MissingTable(collectionGuid, collectionName, localeCode, path);
                using var stream = File.OpenRead(path);
                data = Reader.Read(stream);
            }
            var table = TableDataConverter.FromData(data);
            if (table != null)
            {
                (m_Constructed ??= new HashSet<ResourceTable>()).Add(table);
                var baked = LoadBakedTable(collectionGuid, localeCode);
                if (baked != null)
                {
                    // Grafting is best-effort; a failure must not discard the built table (which is tracked).
                    try
                    {
                        TableDataConverter.GraftNonExportable(table, baked);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to graft baked entries into localization table '{key.Address}': {e.Message}");
                    }
                }
            }
            return table;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load localization table from '{path}': {e.Message}");
            return LoadBakedTable(collectionGuid, localeCode);
        }
    }

    // Baked tables are assets, not tracked in m_Constructed, so Release leaves them alone.
    // The address named one of this provider's collections, so serving nothing is a misconfiguration worth reporting.
    ResourceTable MissingTable(string collectionGuid, string collectionName, string localeCode, string path)
    {
        var baked = LoadBakedTable(collectionGuid, localeCode);
        // Once per address: a binding re-resolves whenever its panel rebuilds, and the fault does not change.
        if (baked == null && (m_WarnedAddresses ??= new HashSet<string>()).Add($"{collectionName}_{localeCode}"))
            Debug.LogWarning($"'{GetType().Name}' serves the localization collection '{collectionName}' but has nothing for locale '{localeCode}': no file at '{path}' and no baked table.");
        return baked;
    }

    ResourceTable LoadBakedTable(string collectionGuid, string localeCode)
    {
        if (m_BakedTables.Count == 0 || string.IsNullOrEmpty(collectionGuid))
            return null;
        var address = BakedTableAddress(collectionGuid, localeCode);
        for (var i = 0; i < m_BakedTables.Count; i++)
        {
            // Ignore case in the locale-code part, matching LocaleIdentifier's comparison semantics.
            var baked = m_BakedTables[i];
            if (baked != null && string.Equals(baked.Address, address, StringComparison.OrdinalIgnoreCase))
                return baked.Table.asset;
        }
        return null;
    }

    // Android StreamingAssets is "jar:file://...!/assets" and WebGL is an http(s) URL; neither supports System.IO.
    static bool IsUrl(string path) => path != null && path.Contains("://");

    static async Awaitable<byte[]> ReadRemote(string url, CancellationToken cancellationToken)
    {
        // Bound the wait, the redirect chain, and the body size so a hosting problem cannot hang the load or balloon memory.
        using var request = UnityWebRequest.Get(url);
        request.timeout = k_RemoteTimeoutSeconds;
        request.redirectLimit = k_RemoteRedirectLimit;
        // Abort on cancellation rather than running to completion; the static callback avoids capturing a closure.
        using (cancellationToken.Register(static state => ((UnityWebRequest)state).Abort(), request))
            await request.SendWebRequest();
        cancellationToken.ThrowIfCancellationRequested();
        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode != 404)
                Debug.LogWarning($"Failed to fetch localization table data from '{url}': {request.error}");
            return null;
        }
        var data = request.downloadHandler.data;
        if (data != null && data.LongLength > k_MaxRemoteBytes)
        {
            Debug.LogWarning($"Localization table data at '{url}' is larger than the {k_MaxRemoteBytes / (1024 * 1024)} MB limit and was not read.");
            return null;
        }
        return data;
    }

    bool TryParseAddress(string address, out string collectionGuid, out string collectionName, out string localeCode)
    {
        // Longest prefix first, so "Menu" does not parse "Menu_Extra_en" as the locale "Extra_en".
        var bestPrefixLength = -1;
        collectionGuid = null;
        collectionName = null;
        localeCode = null;
        for (var i = 0; i < m_Collections.Count; i++)
        {
            var collection = m_Collections[i];
            if (collection == null)
                continue;
            if (!string.IsNullOrEmpty(collection.Guid) && collection.Guid.Length > bestPrefixLength && TryStripPrefix(address, collection.Guid, out var guidLocaleCode))
            {
                bestPrefixLength = collection.Guid.Length;
                collectionGuid = collection.Guid;
                collectionName = collection.Name;
                localeCode = guidLocaleCode;
            }
            if (!string.IsNullOrEmpty(collection.Name) && collection.Name.Length > bestPrefixLength && TryStripPrefix(address, collection.Name, out var nameLocaleCode))
            {
                bestPrefixLength = collection.Name.Length;
                collectionGuid = collection.Guid;
                collectionName = collection.Name;
                localeCode = nameLocaleCode;
            }
        }
        return bestPrefixLength >= 0;
    }

    /// <summary>
    /// The file name a table is stored under, with characters invalid in a file name replaced.
    /// </summary>
    /// <remarks>
    /// The form is <c>{collectionName}_{localeCode}.{extension}</c>. The locale code is lowercased, so a table
    /// authored as <c>pt-BR</c> and a locale requesting <c>pt-br</c> resolve to the same file on case-sensitive
    /// file systems.
    /// </remarks>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="localeCode">The locale code.</param>
    /// <param name="extension">The file extension without the leading dot.</param>
    /// <returns>The file name the table is stored under.</returns>
    public static string FileName(string collectionName, string localeCode, string extension)
        => $"{Sanitize(collectionName)}_{SanitizeSegment(localeCode)?.ToLowerInvariant()}.{extension}";

    /// <summary>
    /// The address a baked table resolves under.
    /// </summary>
    /// <remarks>
    /// The form is <c>{collectionGuid}_{localeCode}</c>. The editor writes it, the provider looks it up; the
    /// format is a private handshake between the two, so it is not public API.
    /// </remarks>
    /// <param name="collectionGuid">The collection guid.</param>
    /// <param name="localeCode">The locale code.</param>
    /// <returns>The address the baked table resolves under.</returns>
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static string BakedTableAddress(string collectionGuid, string localeCode)
        => $"{collectionGuid}_{localeCode}";

    static readonly char[] s_InvalidFileNameChars = Path.GetInvalidFileNameChars();

    const int k_RemoteTimeoutSeconds = 30;
    const int k_RemoteRedirectLimit = 4;
    const long k_MaxRemoteBytes = 64L * 1024 * 1024;

    static string Sanitize(string name) => string.IsNullOrEmpty(name) ? "Table" : SanitizeSegment(name);

    static string SanitizeSegment(string value)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOfAny(s_InvalidFileNameChars) < 0)
            return value;
        foreach (var c in s_InvalidFileNameChars)
            value = value.Replace(c, '_');
        return value;
    }

    static bool TryStripPrefix(string address, string prefix, out string remainder)
    {
        if (address.Length > prefix.Length + 1
            && address[prefix.Length] == '_'
            && address.StartsWith(prefix, StringComparison.Ordinal))
        {
            remainder = address.Substring(prefix.Length + 1);
            return true;
        }
        remainder = null;
        return false;
    }

    static void DestroyObject(Object obj)
    {
        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}
