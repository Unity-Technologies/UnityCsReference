// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    // Adaptive Performance used to generate its assets into "Assets/Adaptive Performance". They now live
    // under "Assets/Settings/AdaptivePerformance" to match where the rest of the Editor puts generated
    // project configuration and to ensure there is not whitespace in the path.
    static class AdaptivePerformanceSettingsMigration
    {
        internal const string k_LegacyRootPath = "Assets/Adaptive Performance";

        // Migration runs at most once per editor session, so a project that cannot be migrated
        // (read-only assets, colliding names) does not retry and re-log on every domain reload.
        const string k_SessionKey_Migrated = "AdaptivePerformance.MigratedSettingsFolder";

        [InitializeOnLoadMethod]
        static void ScheduleMigration()
        {
            // Import workers run InitializeOnLoad too, but must never write to the asset database.
            if (AssetDatabase.IsAssetImportWorkerProcess() || SessionState.GetBool(k_SessionKey_Migrated, false))
                return;

            // Deferred so the migration never runs while assets are still importing or scripts compiling.
            EditorApplication.delayCall -= Migrate;
            EditorApplication.delayCall += Migrate;
        }

        internal static void Migrate()
        {
            EditorApplication.delayCall -= Migrate;
            SessionState.SetBool(k_SessionKey_Migrated, true);

            if (!AssetDatabase.IsValidFolder(k_LegacyRootPath) || !IsRegisteredSettingsInLegacyFolder())
                return;

            string newRootPath = EditorUtilities.GetAssetPathForComponents(EditorUtilities.s_DefaultGeneralSettingsPath);
            if (string.IsNullOrEmpty(newRootPath))
            {
                Debug.LogWarning($"Adaptive Performance was unable to create its settings folder, so the settings in {k_LegacyRootPath} were left in place.");
                return;
            }

            // Not batched with StartAssetEditing/StopAssetEditing: each step queries the database for
            // the folders and assets the previous step created or moved, which needs the changes applied.
            var failures = new List<string>();
            MoveFolderContents(k_LegacyRootPath, newRootPath, failures);
            DeleteFolderIfEmpty(k_LegacyRootPath);
            AssetDatabase.SaveAssets();

            if (failures.Count > 0)
            {
                Debug.LogWarning($"Adaptive Performance was unable to move the following settings from {k_LegacyRootPath} to {newRootPath}: {string.Join(", ", failures)}. Move them manually and delete {k_LegacyRootPath}.");
                return;
            }

            Debug.Log($"Adaptive Performance moved its settings from {k_LegacyRootPath} to {newRootPath}.");
        }

        // A folder with the legacy name may belong to the user rather than Adaptive Performance, so only
        // migrate when it holds the general settings the project actually uses, whatever they are named.
        static bool IsRegisteredSettingsInLegacyFolder()
        {
            if (!EditorBuildSettings.TryGetConfigObject(AdaptivePerformanceGeneralSettings.k_SettingsKey, out AdaptivePerformanceGeneralSettingsPerBuildTarget generalSettings) || generalSettings == null)
                return false;

            return AssetDatabase.GetAssetPath(generalSettings).StartsWith($"{k_LegacyRootPath}/", StringComparison.OrdinalIgnoreCase);
        }

        static void MoveFolderContents(string sourceFolder, string destinationFolder, List<string> failures)
        {
            foreach (var sourceSubFolder in AssetDatabase.GetSubFolders(sourceFolder))
            {
                string folderName = Path.GetFileName(sourceSubFolder);
                string destinationSubFolder = $"{destinationFolder}/{folderName}";

                if (!AssetDatabase.IsValidFolder(destinationSubFolder))
                {
                    string guid = AssetDatabase.CreateFolder(destinationFolder, folderName);
                    if (string.IsNullOrEmpty(guid))
                    {
                        failures.Add(sourceSubFolder);
                        continue;
                    }
                }

                MoveFolderContents(sourceSubFolder, destinationSubFolder, failures);
                DeleteFolderIfEmpty(sourceSubFolder);
            }

            // FindAssets searches recursively, and subfolders can still hold assets that failed to move.
            // Only direct children belong at this level, or a nested asset would be flattened into destinationFolder.
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { sourceFolder }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;

                if (Path.GetDirectoryName(assetPath).Replace('\\', '/') != sourceFolder)
                    continue;

                string destinationPath = $"{destinationFolder}/{Path.GetFileName(assetPath)}";
                string error = AssetDatabase.MoveAsset(assetPath, destinationPath);
                if (!string.IsNullOrEmpty(error))
                    failures.Add(assetPath);
            }
        }

        static void DeleteFolderIfEmpty(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return;

            if (AssetDatabase.GetSubFolders(folderPath).Length > 0 || AssetDatabase.FindAssets(string.Empty, new[] { folderPath }).Length > 0)
                return;

            AssetDatabase.DeleteAsset(folderPath);
        }
    }
}
