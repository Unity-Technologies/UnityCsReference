// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEngine.Assertions;
using UnityEngine.Multiplayer.Internal;
using Unity.Multiplayer.PlayMode.Editor;
using Unity.Multiplayer;

namespace UnityEditor.Multiplayer.Internal
{
    [FilePath("ProjectSettings/Packages/com.unity.dedicated-server/MultiplayerRolesSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class MultiplayerRolesSettings : ScriptableSingleton<MultiplayerRolesSettings>
    {
        static private MultiplayerRoleFlags GetDefaultMultiplayerRoleForBuildProfile(BuildProfile profile)
            => InternalUtilities.IsServerProfile(profile) ? MultiplayerRoleFlags.Server : MultiplayerRoleFlags.Client;

        static private MultiplayerRoleFlags GetDefaultMultiplayerRoleForBuildTarget(NamedBuildTarget namedBuildTarget)
            => namedBuildTarget == NamedBuildTarget.Server ? MultiplayerRoleFlags.Server : MultiplayerRoleFlags.Client;

        // The key used for classic profiles is the platform id.
        [SerializeField] private SerializedDictionary<string, MultiplayerRoleFlags> m_MultiplayerRoleForClassicProfile = new();

        // This is a SerializedDictionary even as a private field so it persists domain reloads.
        private SerializedDictionary<BuildProfile, MultiplayerRoleData> m_MultiplayerRoleForBuildProfile = new(new InstanceIdComparer<BuildProfile>());
        private class InstanceIdComparer<T> : Comparer<T> where T : UnityEngine.Object
        {
            public override int Compare(T x, T y) => x.GetEntityId().CompareTo(y.GetEntityId());
        }

        private static MultiplayerRoleData GetMultiplayerRoleDataInAsset(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var data = (MultiplayerRoleData)null;
            foreach (var asset in assets)
            {
                if (asset is MultiplayerRoleData dataInAsset)
                {
                    if (data == null)
                        data = dataInAsset;
                    else
                    {
                        // If for any reason we end up with multiple role data in the same asset, we remove the extra ones.
                        AssetDatabase.RemoveObjectFromAsset(dataInAsset);
                        EditorUtility.SetDirty(data);
                    }
                }
            }
            return data;
        }

        private static MultiplayerRoleData GetOrCreateRoleDataForBuildProfile(BuildProfile profile)
        {
            if (instance.m_MultiplayerRoleForBuildProfile.TryGetValue(profile, out var data) && data != null)
                return data;

            var assetPath = AssetDatabase.GetAssetPath(profile);
            data = assetPath != null ? GetMultiplayerRoleDataInAsset(assetPath) : null;

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MultiplayerRoleData>();
                data.name = "Multiplayer Role Data";
                data.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                data.multiplayerRole = GetDefaultMultiplayerRoleForBuildProfile(profile);
            }

            instance.m_MultiplayerRoleForBuildProfile[profile] = data;
            instance.Save(true);

            return data;
        }

        private static void AssertValidBuildProfile(BuildProfile buildProfile)
        {
            Assert.IsNotNull(buildProfile);
            Assert.IsTrue((buildProfile.hideFlags & HideFlags.DontSave) == 0, "build profile is not valid due to hide flags");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(buildProfile)), "build profile is not valid as it has not been saved to disk");
        }

        public MultiplayerRoleFlags GetMultiplayerRoleForClassicTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget = StandaloneBuildSubtarget.Default)
        {
            var key = EditorMultiplayerManager.GetUniqueKeyForClassicTarget(buildTarget, subtarget);
            if (m_MultiplayerRoleForClassicProfile.TryGetValue(key, out var mask))
                return mask;

            return GetDefaultMultiplayerRoleForBuildTarget(NamedBuildTarget.FromTargetAndSubtarget(buildTarget, (int)subtarget));
        }

        public MultiplayerRoleFlags GetMultiplayerRoleForBuildProfile(BuildProfile profile)
        {
            AssertValidBuildProfile(profile);
            return GetOrCreateRoleDataForBuildProfile(profile).multiplayerRole;
        }

        public void SetMultiplayerRoleForBuildProfile(BuildProfile profile, MultiplayerRoleFlags mask)
        {
            AssertValidBuildProfile(profile);

            if (GetMultiplayerRoleForBuildProfile(profile) == mask)
                return;

            var data = GetOrCreateRoleDataForBuildProfile(profile);
            data.multiplayerRole = mask;

            if (GetDefaultMultiplayerRoleForBuildProfile(profile) == mask)
                AssetDatabase.RemoveObjectFromAsset(data);
            else if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(data)))
                AssetDatabase.AddObjectToAsset(data, profile);

            EditorUtility.SetDirty(data);
        }

        public void SetMultiplayerRoleForClassicTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget, MultiplayerRoleFlags mask)
        {
            var key = EditorMultiplayerManager.GetUniqueKeyForClassicTarget(buildTarget, subtarget);

            if (GetDefaultMultiplayerRoleForBuildTarget(NamedBuildTarget.FromTargetAndSubtarget(buildTarget, (int)subtarget)) == mask)
                m_MultiplayerRoleForClassicProfile.Remove(key);
            else
                m_MultiplayerRoleForClassicProfile[key] = mask;

            Save(true);
        }
    }
}
