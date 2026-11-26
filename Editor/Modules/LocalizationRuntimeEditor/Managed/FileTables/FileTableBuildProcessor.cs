// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.Build;
using UnityEngine;

namespace Unity.Localization.Editor;

class FileTableBuildProcessor : BuildPlayerProcessor
{
    public override int callbackOrder => 1;

    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        var providers = FileTableGenerator.ChainFileProviders();
        if (providers.Count == 0)
            return;

        var settings = LocalizationEditorSettings.ActiveSettings;
        var settingsWasDirty = settings != null && UnityEditor.EditorUtility.IsDirty(settings);

        var root = FileTableGenerator.DefaultOutputRoot;
        foreach (var provider in providers)
        {
            foreach (var report in FileTableGenerator.Sync(provider, root))
            {
                var count = report.Skipped.Count;
                Debug.Log($"Localization: table '{report.CollectionName} ({report.LocaleCode})' keeps {count} entr{(count == 1 ? "y" : "ies")} in the table asset ({string.Join(", ", report.Skipped.Kinds)}); the table asset is included in the build.");
            }

            var dataDir = FileTableGenerator.ProviderDataDir(root, provider);
            if (Directory.Exists(dataDir) && Directory.GetFiles(dataDir).Length > 0)
                buildPlayerContext.AddAdditionalPathToStreamingAssets(dataDir, provider.SubPath);
        }

        if (!settingsWasDirty && settings != null)
            UnityEditor.EditorUtility.ClearDirty(settings);
    }
}
