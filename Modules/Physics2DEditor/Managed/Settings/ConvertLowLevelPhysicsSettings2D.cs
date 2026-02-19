// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.LowLevelPhysics2D;
using Unity.U2D.Physics;
using System.Collections.Generic;

namespace UnityEditor
{
    static class ConvertLowLevelPhysicsSettings2D
    {
        struct PhysicsCoreSettingsAsset
        {
            public PhysicsCoreSettings2D physicsCoreSettings;
            public string assetPath;
        }

        [RequiredByNativeCode]
        public static void ConvertLegacySettings(EntityId existingSettingsId, out EntityId newSettingsId)
        {
            newSettingsId = EntityId.None;

            // Find any existing settings assets.
            var assetGUIDs = AssetDatabase.FindAssetGUIDs($"t:{nameof(PhysicsLowLevelSettings2D)}");
            if (assetGUIDs.Length == 0)
                return;
            
            // Fetch the assign settings asset path.
            string assignedSettingsAssetPath = string.Empty;
            if (existingSettingsId != EntityId.None)
            {
                var assignedPhysicsLowLevelSettings2D = Resources.EntityIdToObject(existingSettingsId) as PhysicsLowLevelSettings2D;
                assignedSettingsAssetPath = AssetDatabase.GetAssetPath(assignedPhysicsLowLevelSettings2D);
            }

            var assetsToTrash = new List<string>(capacity: assetGUIDs.Length);
            var newSettingsAssets = new List<PhysicsCoreSettingsAsset>(capacity: assetGUIDs.Length);

            // Iterate all the assets.
            foreach (var assetGUID in assetGUIDs)
            {
                // Fetch the asset path.
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);

                // Add to the assets to trash.
                assetsToTrash.Add(assetPath);

                // Convert to physics core settings.
                var physicsCoreSettings = AssetDatabase.LoadAssetByGUID<PhysicsLowLevelSettings2D>(assetGUID).ToPhysicsCoreSettings();
                newSettingsAssets.Add(new PhysicsCoreSettingsAsset { physicsCoreSettings = physicsCoreSettings, assetPath = assetPath });

                // Assign as the new settings if this the currently assigned settings.
                if (assetPath == assignedSettingsAssetPath)
                    newSettingsId = physicsCoreSettings.GetEntityId();
            }

            // Destroy the old settings assets.
            var outFailedPaths = new List<string>();
            AssetDatabase.MoveAssetsToTrash(assetsToTrash.ToArray(), outFailedPaths);

            // Create the new settings assets.
            foreach (var settingsAsset in newSettingsAssets)
            {
                AssetDatabase.CreateAsset(settingsAsset.physicsCoreSettings, AssetDatabase.GenerateUniqueAssetPath(settingsAsset.assetPath));
            }
            AssetDatabase.SaveAssets();
        }
    }
}
