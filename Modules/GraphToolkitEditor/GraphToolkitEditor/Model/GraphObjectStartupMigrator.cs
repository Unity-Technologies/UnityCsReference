// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    // Patches all graph assets once per editor session so their corelib assembly name matches the
    // running runtime. Assets already in the import cache when the editor starts do not pass through
    // the standard import pipeline, so they require this explicit scan on domain load.
    // File patching runs synchronously so disk state is correct before window restoration;
    // reimports are deferred to delayCall to avoid calling ImportAsset during domain reload.
    [InitializeOnLoad]
    static class GraphObjectStartupMigrator
    {
        internal const string k_MigratedSessionKey = "GraphToolkit.GraphObjectStartupMigrator.Migrated";

        static GraphObjectStartupMigrator()
        {
            var toReimport = MigrateOncePerSession();
            if (toReimport.Count > 0)
                EditorApplication.delayCall += () => Reimport(toReimport);
        }

        internal static HashSet<string> MigrateOncePerSession()
        {
            // The runtime cannot change without restarting the editor, which clears SessionState.
            if (SessionState.GetBool(k_MigratedSessionKey, false))
                return new HashSet<string>();

            var toReimport = CollectAndMigrateFiles();
            SessionState.SetBool(k_MigratedSessionKey, true);
            return toReimport;
        }

        static HashSet<string> CollectAndMigrateFiles()
        {
            var paths = new HashSet<string>();

            foreach (var extension in GraphObjectFactory.GetExtensions())
            {
                foreach (var guid in AssetDatabase.FindAssets($"glob:\"*.{extension}\""))
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(GraphObject)}"))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            var toReimport = new HashSet<string>();
            foreach (var path in paths)
            {
                if (GraphObject.MigrateTypeHandles(path))
                    toReimport.Add(path);
            }

            return toReimport;
        }

        static void Reimport(HashSet<string> paths)
        {
            foreach (var path in paths)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }
}
