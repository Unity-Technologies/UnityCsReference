// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.AssetImporters;

namespace UnityEditor.Search
{
    [Flags]
    enum IndexingOptions : byte
    {
        Types        = 1 << 0,      // Index type information about objects
        Properties   = 1 << 1,      // Index serialized properties of objects
        Extended     = 1 << 2,      // Index all sub-assets and objects as new documents
        Dependencies = 1 << 3,      // Index object dependencies (i.e. ref:<name>)
    }

    abstract class SearchIndexEntryImporter : ScriptedImporter
    {
        public const int version = SearchIndexEntry.version;

        protected abstract IndexingOptions options { get; }

        private SearchDatabase.Options GetOptions()
        {
            return new SearchDatabase.Options()
            {
                types = options.HasFlag(IndexingOptions.Types),
                properties = options.HasFlag(IndexingOptions.Properties),
                extended = options.HasFlag(IndexingOptions.Extended),
                dependencies = options.HasFlag(IndexingOptions.Dependencies)
            };
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var settings = new SearchDatabase.Settings { type = "asset", options = GetOptions() };

            try
            {
                var indexer = SearchDatabase.CreateIndexer(settings);
                try
                {
                    ctx.DependsOnCustomDependency(GetType().GUID.ToString("N"));
                    ctx.DependsOnCustomDependency(nameof(CustomObjectIndexerAttribute));
                    indexer.IndexDocument(ctx.assetPath, false);
                    indexer.ApplyUnsorted();
                }
                catch (Exception ex)
                {
                    ctx.LogImportError($"Failed to build search index for {ctx.assetPath}\n{ex}");
                }

                var indexArtifactPath = ctx.GetOutputArtifactFilePath($"{(int)options:X}.index".ToLowerInvariant());
                using (var fileStream = new FileStream(indexArtifactPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    indexer.Write(fileStream);
            }
            catch (Exception ex)
            {
                ctx.LogImportError(ex.Message);
            }
        }

        public static Type GetIndexImporterType(int hash)
        {
            switch (hash)
            {
                case 0x00: return typeof(ASIEI00); case 0x01: return typeof(ASIEI01); case 0x02: return typeof(ASIEI02); case 0x03: return typeof(ASIEI03);
                case 0x04: return typeof(ASIEI04); case 0x05: return typeof(ASIEI05); case 0x06: return typeof(ASIEI06); case 0x07: return typeof(ASIEI07);
                case 0x08: return typeof(ASIEI08); case 0x09: return typeof(ASIEI09); case 0x0A: return typeof(ASIEI0A); case 0x0B: return typeof(ASIEI0B);
                case 0x0C: return typeof(ASIEI0C); case 0x0D: return typeof(ASIEI0D); case 0x0E: return typeof(ASIEI0E); case 0x0F: return typeof(ASIEI0F);
            }

            throw new NotSupportedException($"No index importer for ({hash})");
        }
    }

    abstract class ASIEI : SearchIndexEntryImporter {}

    [ScriptedImporter(version, "~aindex00", AllowCaching = true)] class ASIEI00 : ASIEI { protected override IndexingOptions options => 0x00; }
    [ScriptedImporter(version, "~aindex01", AllowCaching = true)] class ASIEI01 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x01; }
    [ScriptedImporter(version, "~aindex02", AllowCaching = true)] class ASIEI02 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x02; }
    [ScriptedImporter(version, "~aindex03", AllowCaching = true)] class ASIEI03 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x03; }
    [ScriptedImporter(version, "~aindex04", AllowCaching = true)] class ASIEI04 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x04; }
    [ScriptedImporter(version, "~aindex05", AllowCaching = true)] class ASIEI05 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x05; }
    [ScriptedImporter(version, "~aindex06", AllowCaching = true)] class ASIEI06 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x06; }
    [ScriptedImporter(version, "~aindex07", AllowCaching = true)] class ASIEI07 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x07; }
    [ScriptedImporter(version, "~aindex08", AllowCaching = true)] class ASIEI08 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x08; }
    [ScriptedImporter(version, "~aindex09", AllowCaching = true)] class ASIEI09 : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x09; }
    [ScriptedImporter(version, "~aindex0A", AllowCaching = true)] class ASIEI0A : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x0A; }
    [ScriptedImporter(version, "~aindex0B", AllowCaching = true)] class ASIEI0B : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x0B; }
    [ScriptedImporter(version, "~aindex0C", AllowCaching = true)] class ASIEI0C : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x0C; }
    [ScriptedImporter(version, "~aindex0D", AllowCaching = true)] class ASIEI0D : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x0D; }
    [ScriptedImporter(version, "~aindex0E", AllowCaching = true)] class ASIEI0E : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x0E; }
    [ScriptedImporter(version, "~aindex0F", AllowCaching = true)] class ASIEI0F : ASIEI { protected override IndexingOptions options => (IndexingOptions)0x0F; }
}
