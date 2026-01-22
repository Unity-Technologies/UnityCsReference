// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING
using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Profiling;

namespace UnityEditor.Search
{
    abstract class SearchIndexArtifactImporter : ScriptedImporter
    {
        public const int Version = SearchIndexArtifactStorage.DefaultVersion | (SearchIndexer.version << 16);

        public static string GetCustomDependencyName(Type type)
        {
            return type.FullName;
        }

        public abstract IndexingOptions options { get; }

        internal static SearchDatabase.Options GetOptions(IndexingOptions options, string assetPath)
        {
            return new SearchDatabase.Options()
            {
                types = options.HasFlag(IndexingOptions.Types),
                properties = options.HasFlag(IndexingOptions.Properties),
                extended = SearchDatabase.SupportsExtendedIndexing(assetPath) && options.HasFlag(IndexingOptions.Extended),
                dependencies = options.HasFlag(IndexingOptions.Dependencies)
            };
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var settings = new SearchDatabase.Settings { type = "asset", options = GetOptions(options, ctx.assetPath) };
            using var indexer = SearchDatabase.CreateIndexer(settings, null, new SearchIndexArtifactStorage());
            try
            {
                using var pp = new EditorPerformanceMarker("SearchImporter").Auto();
                indexer.Start();
                indexer.IndexDocument(ctx.assetPath, false);
                indexer.Finish(removedDocuments: null);

                var indexArtifactExtension = $"{(int)options:X}.index".ToLowerInvariant();

                using (var memoryStream = new MemoryStream())
                {
                    indexer.storage.Write(memoryStream);
                    ctx.SetOutputArtifactData(indexArtifactExtension, memoryStream.ToArray());
                }

                ctx.DependsOnSourceAsset(ctx.assetPath);
                var customDependencyName = GetCustomDependencyName(GetType());
                ctx.DependsOnCustomDependency(customDependencyName);
                ctx.DependsOnCustomDependency(nameof(CustomObjectIndexerAttribute));

            }
            catch (Exception ex)
            {
                ctx.LogImportError($"Failed to build search index for {ctx.assetPath}\n{ex}");
            }
        }

        public static Type GetIndexImporterType(int hash)
        {
            switch (hash)
            {
                case 0x00: return typeof(ASIAI00);
                case 0x01: return typeof(ASIAI01);
                case 0x02: return typeof(ASIAI02);
                case 0x03: return typeof(ASIAI03);
                case 0x04: return typeof(ASIAI04);
                case 0x05: return typeof(ASIAI05);
                case 0x06: return typeof(ASIAI06);
                case 0x07: return typeof(ASIAI07);
                case 0x08: return typeof(ASIAI08);
                case 0x09: return typeof(ASIAI09);
                case 0x0A: return typeof(ASIAI0A);
                case 0x0B: return typeof(ASIAI0B);
                case 0x0C: return typeof(ASIAI0C);
                case 0x0D: return typeof(ASIAI0D);
                case 0x0E: return typeof(ASIAI0E);
                case 0x0F: return typeof(ASIAI0F);
            }

            throw new NotSupportedException($"No index importer for ({hash})");
        }
    }

    abstract class ASIAI : SearchIndexArtifactImporter { }

    [ScriptedImporter(Version, "~arindex00", AllowCaching = true)] class ASIAI00 : ASIAI { public override IndexingOptions options => 0x00; }
    [ScriptedImporter(Version, "~arindex01", AllowCaching = true)] class ASIAI01 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x01; }
    [ScriptedImporter(Version, "~arindex02", AllowCaching = true)] class ASIAI02 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x02; }
    [ScriptedImporter(Version, "~arindex03", AllowCaching = true)] class ASIAI03 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x03; }
    [ScriptedImporter(Version, "~arindex04", AllowCaching = true)] class ASIAI04 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x04; }
    [ScriptedImporter(Version, "~arindex05", AllowCaching = true)] class ASIAI05 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x05; }
    [ScriptedImporter(Version, "~arindex06", AllowCaching = true)] class ASIAI06 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x06; }
    [ScriptedImporter(Version, "~arindex07", AllowCaching = true)] class ASIAI07 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x07; }
    [ScriptedImporter(Version, "~arindex08", AllowCaching = true)] class ASIAI08 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x08; }
    [ScriptedImporter(Version, "~arindex09", AllowCaching = true)] class ASIAI09 : ASIAI { public override IndexingOptions options => (IndexingOptions)0x09; }
    [ScriptedImporter(Version, "~arindex0A", AllowCaching = true)] class ASIAI0A : ASIAI { public override IndexingOptions options => (IndexingOptions)0x0A; }
    [ScriptedImporter(Version, "~arindex0B", AllowCaching = true)] class ASIAI0B : ASIAI { public override IndexingOptions options => (IndexingOptions)0x0B; }
    [ScriptedImporter(Version, "~arindex0C", AllowCaching = true)] class ASIAI0C : ASIAI { public override IndexingOptions options => (IndexingOptions)0x0C; }
    [ScriptedImporter(Version, "~arindex0D", AllowCaching = true)] class ASIAI0D : ASIAI { public override IndexingOptions options => (IndexingOptions)0x0D; }
    [ScriptedImporter(Version, "~arindex0E", AllowCaching = true)] class ASIAI0E : ASIAI { public override IndexingOptions options => (IndexingOptions)0x0E; }
    [ScriptedImporter(Version, "~arindex0F", AllowCaching = true)] class ASIAI0F : ASIAI { public override IndexingOptions options => (IndexingOptions)0x0F; }
}
