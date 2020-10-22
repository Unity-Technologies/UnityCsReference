// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace UnityEditor.Search
{
    [Flags]
    enum IndexingOptions : byte
    {
        Types        = 1 << 0,      // Index type information about objects
        Properties   = 1 << 1,      // Index serialized properties of objects
        Extended     = 1 << 2,      // Index as many properties as possible
        Dependencies = 1 << 3,      // Index object dependencies (i.e. ref:<name>)
    }

    abstract class SearchIndexEntryImporter : ScriptedImporter
    {
        // 14- Add extended options to index as many properties as possible
        // 15- Add a dependency on the container folder of the asset so it gets re-indexed when the folder gets renamed
        public const int version = (15 << 18) ^ SearchIndexEntry.version;

        protected abstract string type { get; }
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
            var settings = new SearchDatabase.Settings
            {
                guid = null,
                root = null,
                roots = null,
                source = null,
                name = null,
                baseScore = 0,
                excludes = null,
                includes = null,

                type = type,
                options = GetOptions(),
            };

            EditorApplication.LockReloadAssemblies();
            try
            {
                var indexer = SearchDatabase.CreateIndexer(settings);
                indexer.IndexDocument(ctx.assetPath, false);
                indexer.ApplyUnsorted();

                var indexArtifactPath = ctx.GetResultPath($"{type}.{(int)options:X}.index".ToLowerInvariant());
                using (var fileStream = new FileStream(indexArtifactPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    indexer.Write(fileStream);

                Console.WriteLine($"Generated {type} ({GetType().Name}) {indexArtifactPath} for {ctx.assetPath} with {options}");

                ctx.DependsOnSourceAsset(Path.GetDirectoryName(ctx.assetPath).Replace("\\", "/"));
                ctx.DependsOnCustomDependency(GetType().GUID.ToString("N"));
                ctx.DependsOnCustomDependency(nameof(CustomObjectIndexerAttribute));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ctx.LogImportError(ex.Message);
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        public static Type GetIndexImporterType(string type, int hash)
        {
            if (type == "asset")
            {
                switch (hash)
                {
                    case 0x00: return typeof(ASIEI00); case 0x01: return typeof(ASIEI01); case 0x02: return typeof(ASIEI02); case 0x03: return typeof(ASIEI03);
                    case 0x04: return typeof(ASIEI04); case 0x05: return typeof(ASIEI05); case 0x06: return typeof(ASIEI06); case 0x07: return typeof(ASIEI07);
                    case 0x08: return typeof(ASIEI08); case 0x09: return typeof(ASIEI09); case 0x0A: return typeof(ASIEI0A); case 0x0B: return typeof(ASIEI0B);
                    case 0x0C: return typeof(ASIEI0C); case 0x0D: return typeof(ASIEI0D); case 0x0E: return typeof(ASIEI0E); case 0x0F: return typeof(ASIEI0F);
                }
            }
            else if (type == "prefab")
            {
                switch (hash)
                {
                    case 0x00: return typeof(PSIEI00); case 0x01: return typeof(PSIEI01); case 0x02: return typeof(PSIEI02); case 0x03: return typeof(PSIEI03);
                    case 0x04: return typeof(PSIEI04); case 0x05: return typeof(PSIEI05); case 0x06: return typeof(PSIEI06); case 0x07: return typeof(PSIEI07);
                    case 0x08: return typeof(PSIEI08); case 0x09: return typeof(PSIEI09); case 0x0A: return typeof(PSIEI0A); case 0x0B: return typeof(PSIEI0B);
                    case 0x0C: return typeof(PSIEI0C); case 0x0D: return typeof(PSIEI0D); case 0x0E: return typeof(PSIEI0E); case 0x0F: return typeof(PSIEI0F);
                }
            }
            else if (type == "scene")
            {
                switch (hash)
                {
                    case 0x00: return typeof(SSIEI00); case 0x01: return typeof(SSIEI01); case 0x02: return typeof(SSIEI02); case 0x03: return typeof(SSIEI03);
                    case 0x04: return typeof(SSIEI04); case 0x05: return typeof(SSIEI05); case 0x06: return typeof(SSIEI06); case 0x07: return typeof(SSIEI07);
                    case 0x08: return typeof(SSIEI08); case 0x09: return typeof(SSIEI09); case 0x0A: return typeof(SSIEI0A); case 0x0B: return typeof(SSIEI0B);
                    case 0x0C: return typeof(SSIEI0C); case 0x0D: return typeof(SSIEI0D); case 0x0E: return typeof(SSIEI0E); case 0x0F: return typeof(SSIEI0F);
                }
            }

            throw new NotSupportedException($"No index importer for {type} ({hash})");
        }
    }

    abstract class ASIEI : SearchIndexEntryImporter { protected override string type => "asset"; }
    abstract class PSIEI : SearchIndexEntryImporter { protected override string type => "prefab"; }
    abstract class SSIEI : SearchIndexEntryImporter { protected override string type => "scene"; }

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

    [ScriptedImporter(version, "~pindex00", AllowCaching = true)] class PSIEI00 : PSIEI { protected override IndexingOptions options => 0x00; }
    [ScriptedImporter(version, "~pindex01", AllowCaching = true)] class PSIEI01 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x01; }
    [ScriptedImporter(version, "~pindex02", AllowCaching = true)] class PSIEI02 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x02; }
    [ScriptedImporter(version, "~pindex03", AllowCaching = true)] class PSIEI03 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x03; }
    [ScriptedImporter(version, "~pindex04", AllowCaching = true)] class PSIEI04 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x04; }
    [ScriptedImporter(version, "~pindex05", AllowCaching = true)] class PSIEI05 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x05; }
    [ScriptedImporter(version, "~pindex06", AllowCaching = true)] class PSIEI06 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x06; }
    [ScriptedImporter(version, "~pindex07", AllowCaching = true)] class PSIEI07 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x07; }
    [ScriptedImporter(version, "~pindex08", AllowCaching = true)] class PSIEI08 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x08; }
    [ScriptedImporter(version, "~pindex09", AllowCaching = true)] class PSIEI09 : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x09; }
    [ScriptedImporter(version, "~pindex0A", AllowCaching = true)] class PSIEI0A : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x0A; }
    [ScriptedImporter(version, "~pindex0B", AllowCaching = true)] class PSIEI0B : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x0B; }
    [ScriptedImporter(version, "~pindex0C", AllowCaching = true)] class PSIEI0C : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x0C; }
    [ScriptedImporter(version, "~pindex0D", AllowCaching = true)] class PSIEI0D : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x0D; }
    [ScriptedImporter(version, "~pindex0E", AllowCaching = true)] class PSIEI0E : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x0E; }
    [ScriptedImporter(version, "~pindex0F", AllowCaching = true)] class PSIEI0F : PSIEI { protected override IndexingOptions options => (IndexingOptions)0x0F; }

    [ScriptedImporter(version, "~sindex00", AllowCaching = true)] class SSIEI00 : SSIEI { protected override IndexingOptions options => 0x00; }
    [ScriptedImporter(version, "~sindex01", AllowCaching = true)] class SSIEI01 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x01; }
    [ScriptedImporter(version, "~sindex02", AllowCaching = true)] class SSIEI02 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x02; }
    [ScriptedImporter(version, "~sindex03", AllowCaching = true)] class SSIEI03 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x03; }
    [ScriptedImporter(version, "~sindex04", AllowCaching = true)] class SSIEI04 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x04; }
    [ScriptedImporter(version, "~sindex05", AllowCaching = true)] class SSIEI05 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x05; }
    [ScriptedImporter(version, "~sindex06", AllowCaching = true)] class SSIEI06 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x06; }
    [ScriptedImporter(version, "~sindex07", AllowCaching = true)] class SSIEI07 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x07; }
    [ScriptedImporter(version, "~sindex08", AllowCaching = true)] class SSIEI08 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x08; }
    [ScriptedImporter(version, "~sindex09", AllowCaching = true)] class SSIEI09 : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x09; }
    [ScriptedImporter(version, "~sindex0A", AllowCaching = true)] class SSIEI0A : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x0A; }
    [ScriptedImporter(version, "~sindex0B", AllowCaching = true)] class SSIEI0B : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x0B; }
    [ScriptedImporter(version, "~sindex0C", AllowCaching = true)] class SSIEI0C : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x0C; }
    [ScriptedImporter(version, "~sindex0D", AllowCaching = true)] class SSIEI0D : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x0D; }
    [ScriptedImporter(version, "~sindex0E", AllowCaching = true)] class SSIEI0E : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x0E; }
    [ScriptedImporter(version, "~sindex0F", AllowCaching = true)] class SSIEI0F : SSIEI { protected override IndexingOptions options => (IndexingOptions)0x0F; }
}
