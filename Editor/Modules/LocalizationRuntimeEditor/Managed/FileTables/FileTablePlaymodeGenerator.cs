// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Localization.Providers.FileTables;

namespace Unity.Localization.Editor;

static class FileTablePlaymodeGenerator
{
    internal static void PrepareForPlayMode()
    {
        var providers = FileTableGenerator.ChainFileProviders();
        if (providers.Count == 0)
            return;

        List<FileTableProvider> toGenerate = null;
        foreach (var provider in providers)
        {
            if (provider.PlayMode == PlayModeSource.SourceAssets)
                provider.SetSourceTables(BuildSourceMap(provider));
            else
                (toGenerate ??= new List<FileTableProvider>()).Add(provider);
        }

        if (toGenerate == null)
            return;

        var root = FileTableGenerator.DefaultOutputRoot;
        FileTableGenerator.Sync(toGenerate, root);
        foreach (var provider in toGenerate)
            provider.SetEditorBasePathOverride(FileTableGenerator.ProviderDataDir(root, provider));
    }

    // Edit mode always serves the table assets, whatever PlayMode says: an authoring preview should show what is
    // authored, and generating files for every preview would write to Library for nothing.
    internal static void PrepareForEditMode()
    {
        foreach (var provider in FileTableGenerator.ChainFileProviders())
        {
            provider.ClearEditorState();
            provider.SetSourceTables(BuildSourceMap(provider));
        }
    }

    static Dictionary<string, ResourceTable> BuildSourceMap(FileTableProvider provider)
    {
        // Addresses are matched without case elsewhere in the provider, so a pt-BR table answers a pt-br request.
        var map = new Dictionary<string, ResourceTable>(StringComparer.OrdinalIgnoreCase);
        foreach (var collection in FileTableGenerator.CollectionsFor(provider))
        {
            var shared = collection.SharedData;
            var guid = shared != null && !shared.TableCollectionNameGuid.Empty() ? shared.TableCollectionNameGuid.ToString() : null;
            var name = collection.TableCollectionName;
            foreach (var table in collection.Tables)
            {
                // Match the build: a disabled locale's table is not served in Play Mode either.
                if (table == null || !AssetProviderEditor.IsLocaleEnabled(table.LocaleIdentifier))
                    continue;
                var locale = table.LocaleIdentifier.Code;
                if (!string.IsNullOrEmpty(guid))
                    map[$"{guid}_{locale}"] = table;
                if (!string.IsNullOrEmpty(name))
                    map[$"{name}_{locale}"] = table;
            }
        }
        return map;
    }

    // Returning to edit mode, or before an assembly reload: drop generated instances and free the loaded assets.
    internal static void ClearAndRelease()
    {
        foreach (var provider in FileTableGenerator.ChainFileProviders())
            provider.ClearEditorState();
        LocalizationEditorSettings.ActiveSettings?.Database?.ReleaseAllAssets();
    }
}
