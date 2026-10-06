// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.DedicatedServer.Editor.Internal;
using Unity.Multiplayer;
using Unity.Multiplayer.Editor;
using Unity.Profiling;
using UnityEditor.Build.Content;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor.Multiplayer.Internal
{
    class MultiplayerScenePostprocessor : AssetPostprocessor
    {
        // Has to be kept in sync with "Modules/MultiplayerEditor/MultiplayerAssetDatabaseDependencies.cpp"
        const string k_MultiplayerRoleEnabledDependencyKey = "MultiplayerRole.MultiplayerRoleEnabled";
        // Has to be kept in sync with "Modules/MultiplayerEditor/MultiplayerAssetDatabaseDependencies.cpp"
        const string k_MultiplayerPlayerRoleDependencyKey = "MultiplayerRole.MultiplayerActiveRole";

        private static readonly ProfilerMarker s_OnProcessSceneMarker = new ProfilerMarker("MultiplayerRolesScenePostprocessor.OnProcessScene");

        public override uint GetVersion()
        {
            return 1;
        }

        void OnProcessScene(Scene scene, SceneImportContext sceneContext)
        {
            context.DependsOnCustomDependency(k_MultiplayerRoleEnabledDependencyKey);
            if(!EditorMultiplayerManager.enableMultiplayerRoles)
                return;

            context.DependsOnCustomDependency(k_MultiplayerPlayerRoleDependencyKey);
            MultiplayerRoleFlags multiplayerRole;
            if (sceneContext.loadingReason == ProcessSceneMode.PlayerBuild)
            {
                // Get the actual multiplayer role from the active buildprofile/buildtarget
                // because EditorMultiplayerManager.activeMultiplayerRoleMask is static and not serialized
                // which prevents import workers from getting the actual value.
                var profile = BuildProfile.GetActiveBuildProfile();
                if (profile != null)
                {
                    multiplayerRole = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(profile);
                }
                else if (InternalUtility.IsStandalonePlatform(EditorUserBuildSettings.activeBuildTarget))
                {
                    multiplayerRole = EditorMultiplayerRolesManager.GetMultiplayerRoleForClassicTarget(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.standaloneBuildSubtarget);
                }
                else
                {
                    multiplayerRole = EditorMultiplayerRolesManager.GetMultiplayerRoleForClassicTarget(EditorUserBuildSettings.activeBuildTarget);
                }
            }
            else
            {
                // when loading a scene for playmode, read it directly from the manager
                // because the value might not be synced with the actual build profile.
                multiplayerRole = EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask;
            }

            if(EditorMultiplayerRolesManager.EnableSafetyChecks)
            {
                using var marker = s_OnProcessSceneMarker.Auto();
                var referenceTracker = new ReferenceTracker(multiplayerRole, scene);
                referenceTracker.Collect();
                referenceTracker.WarnBrokenReferencesIfNeeded();
            }

            EditorMultiplayerManager.StripSceneObjects(multiplayerRole, scene);

            // also look into dont destroy on load scene if awake ran given objects might have moved scenes.
            if (sceneContext.awakeDidRun)
            {
                var ddolScene = EditorSceneManager.GetDontDestroyOnLoadScene();
                EditorMultiplayerManager.StripSceneObjects(multiplayerRole, ddolScene);
            }
        }
    }
}
