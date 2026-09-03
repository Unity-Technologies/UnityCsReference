// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Localization.Providers;
using Unity.Localization.Providers.FileTables;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.Localization.Editor;

static class FileTableGenerator
{
    [Serializable]
    class GenerationManifest
    {
        public string SchemaVersion = "1";
        public List<CollectionHash> Collections = new();
    }

    [Serializable]
    struct CollectionHash
    {
        public string Guid;
        public string Hash;
    }

    public static string DefaultOutputRoot =>
        Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "com.unity.localization");

    public static string ProviderDataDir(string outputRoot, FileTableProvider provider) =>
        Path.Combine(outputRoot, ProviderFolder(provider), "data");

    static string ManifestPath(string outputRoot, FileTableProvider provider) =>
        Path.Combine(outputRoot, ProviderFolder(provider), "generation-manifest.json");

    static string ProviderFolder(FileTableProvider provider)
    {
        // Id is a default interface member, so it is only reachable through IAssetProvider.
        var id = ((IAssetProvider)provider).Id;
        if (string.IsNullOrEmpty(id))
            return "provider";
        foreach (var c in Path.GetInvalidFileNameChars())
            id = id.Replace(c, '_');
        return id.Trim('.', ' ') is { Length: > 0 } trimmed ? trimmed : "provider";
    }

    public static List<FileTableProvider> ChainFileProviders()
    {
        var result = new List<FileTableProvider>();
        foreach (var provider in AssetProviderEditors.ChainProviders())
        {
            if (provider is FileTableProvider fileProvider)
                result.Add(fileProvider);
        }
        return result;
    }

    public static List<ResourceTableCollection> CollectionsFor(FileTableProvider provider)
    {
        var id = ((IAssetProvider)provider).Id;
        var result = new List<ResourceTableCollection>();
        foreach (var collection in AssetProviderEditors.FindAllCollections())
        {
            if (collection != null && collection.ProviderId == id)
                result.Add(collection);
        }
        return result;
    }

    // One table kept in the build because some of its entries cannot ship as file data.
    internal readonly struct BakedTableReport
    {
        internal BakedTableReport(string collectionName, string localeCode, TableDataBuilder.SkippedEntries skipped)
        {
            CollectionName = collectionName;
            LocaleCode = localeCode;
            Skipped = skipped;
        }

        internal string CollectionName { get; }
        internal string LocaleCode { get; }
        internal TableDataBuilder.SkippedEntries Skipped { get; }
    }

    public static void Sync(IEnumerable<FileTableProvider> providers, string outputRoot)
    {
        foreach (var provider in providers)
            Sync(provider, outputRoot);
    }

    // Also refreshes the provider's baked tables: those holding entries that cannot ship as file data stay in the build.
    internal static List<BakedTableReport> Sync(FileTableProvider provider, string outputRoot)
    {
        var reports = new List<BakedTableReport>();
        var writer = (AssetProviderEditors.GetEditor(provider) as FileTableProviderEditor)?.Writer;
        if (writer == null)
        {
            Debug.LogWarning($"No file writer is registered for '{provider.GetType().Name}', so its localization data files are not generated and its tables will not resolve in a player.");
            return reports;
        }

        var dataDir = ProviderDataDir(outputRoot, provider);
        var manifestPath = ManifestPath(outputRoot, provider);
        var recorded = LoadManifest(manifestPath);
        var current = new Dictionary<string, string>();
        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var baked = new List<FileTableProvider.BakedTable>();
        var ext = provider.FileExtension;

        foreach (var collection in CollectionsFor(provider))
        {
            var shared = collection.SharedData;
            if (shared == null || shared.TableCollectionNameGuid.Empty())
                continue;
            var guid = shared.TableCollectionNameGuid.ToString();

            // Keep the runtime address lookup in sync so it ships with the built settings.
            provider.AddOwnedCollection(guid, collection.TableCollectionName);

            var fileNames = ListPool<string>.Get();
            foreach (var table in collection.Tables)
            {
                // A disabled locale is excluded from builds, so its file is neither written nor kept as a baked asset.
                if (table == null || !AssetProviderEditor.IsLocaleEnabled(table.LocaleIdentifier))
                    continue;
                fileNames.Add(FileTableProvider.FileName(collection.TableCollectionName, table.LocaleIdentifier.Code, ext));

                var skipped = TableDataBuilder.Classify(table, ExportKind.Player);
                if (skipped.Count > 0)
                {
                    baked.Add(new FileTableProvider.BakedTable
                    {
                        Address = FileTableProvider.BakedTableAddress(guid, table.LocaleIdentifier.Code),
                        Table = new LazyLoadReference<ResourceTable>(table)
                    });
                    reports.Add(new BakedTableReport(collection.TableCollectionName, table.LocaleIdentifier.Code, skipped));
                }
            }
            expected.UnionWith(fileNames);

            var hash = ComputeCollectionHash(collection);
            current[guid] = hash;

            var upToDate = recorded.TryGetValue(guid, out var previous) && previous == hash && AllExist(dataDir, fileNames);
            ListPool<string>.Release(fileNames);
            if (upToDate)
                continue;

            Directory.CreateDirectory(dataDir);
            foreach (var table in collection.Tables)
            {
                if (table == null || !AssetProviderEditor.IsLocaleEnabled(table.LocaleIdentifier))
                    continue;
                var path = Path.Combine(dataDir, FileTableProvider.FileName(collection.TableCollectionName, table.LocaleIdentifier.Code, ext));
                WriteIfChanged(writer, table, path);
            }
        }

        ApplyBakedTables(provider, baked);
        Prune(dataDir, expected);
        SaveManifest(manifestPath, current);
        return reports;
    }

    static void ApplyBakedTables(FileTableProvider provider, List<FileTableProvider.BakedTable> baked)
    {
        if (BakedListsEqual(provider.BakedTables, baked))
            return;
        provider.SetBakedTables(baked);
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings != null)
            EditorUtility.SetDirty(settings);
    }

    static bool BakedListsEqual(IReadOnlyList<FileTableProvider.BakedTable> current, List<FileTableProvider.BakedTable> baked)
    {
        if (current.Count != baked.Count)
            return false;
        for (var i = 0; i < current.Count; i++)
        {
            if (current[i] == null || baked[i] == null)
            {
                if (!ReferenceEquals(current[i], baked[i]))
                    return false;
                continue;
            }
            if (!string.Equals(current[i].Address, baked[i].Address, StringComparison.Ordinal)
                || current[i].Table.entityId != baked[i].Table.entityId)
                return false;
        }
        return true;
    }

    static void WriteIfChanged(ITableFileWriter writer, ResourceTable table, string path)
    {
        byte[] bytes;
        using (var buffer = new MemoryStream())
        {
            // The warning-free overload: baked entries are supported here, Sync already reports them.
            writer.Write(TableDataBuilder.ToData(table, ExportKind.Player, out _), buffer);
            bytes = buffer.ToArray();
        }
        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            if (info.Length == bytes.Length && BytesEqual(File.ReadAllBytes(path), bytes))
                return;
        }
        File.WriteAllBytes(path, bytes);
    }

    static string ComputeCollectionHash(ResourceTableCollection collection)
    {
        // Joined rather than folded together with Hash128.Append: that overload set is declared against NativeArray,
        // which this module does not reference. The value only has to be stable and comparable.
        var text = new StringBuilder();
        AppendAssetHash(text, collection);
        AppendAssetHash(text, collection.SharedData);
        var tables = collection.Tables;
        for (var i = 0; i < tables.Count; i++)
            AppendAssetHash(text, tables[i]);
        return text.ToString();
    }

    static void AppendAssetHash(StringBuilder text, UnityEngine.Object asset)
    {
        if (asset == null)
            return;
        var path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path))
            text.Append(AssetDatabase.GetAssetDependencyHash(path).ToString()).Append(';');
    }

    static void Prune(string dataDir, HashSet<string> expected)
    {
        if (!Directory.Exists(dataDir))
            return;
        foreach (var path in Directory.GetFiles(dataDir))
        {
            if (!expected.Contains(Path.GetFileName(path)))
                File.Delete(path);
        }
    }

    static bool AllExist(string dataDir, List<string> fileNames)
    {
        for (var i = 0; i < fileNames.Count; i++)
        {
            if (!File.Exists(Path.Combine(dataDir, fileNames[i])))
                return false;
        }
        return true;
    }

    static bool BytesEqual(byte[] a, byte[] b) => System.MemoryExtensions.SequenceEqual<byte>(a, b);

    static Dictionary<string, string> LoadManifest(string path)
    {
        var result = new Dictionary<string, string>();
        if (!File.Exists(path))
            return result;
        try
        {
            var manifest = JsonUtility.FromJson<GenerationManifest>(File.ReadAllText(path));
            if (manifest?.Collections != null)
            {
                foreach (var entry in manifest.Collections)
                {
                    if (!string.IsNullOrEmpty(entry.Guid))
                        result[entry.Guid] = entry.Hash;
                }
            }
        }
        catch (Exception e)
        {
            // A missing or unreadable manifest just forces a full regenerate.
            Debug.LogWarning($"Could not read localization generation manifest '{path}': {e.Message}");
        }
        return result;
    }

    static void SaveManifest(string path, Dictionary<string, string> hashes)
    {
        var manifest = new GenerationManifest();
        foreach (var pair in hashes)
            manifest.Collections.Add(new CollectionHash { Guid = pair.Key, Hash = pair.Value });
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(manifest, true));
    }
}
