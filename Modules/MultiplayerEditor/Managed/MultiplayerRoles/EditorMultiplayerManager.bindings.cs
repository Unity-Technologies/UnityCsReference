// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.DedicatedServer.Editor.Internal;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using Unity.Multiplayer;
using Unity.Multiplayer.Editor;
using UnityEngine.SceneManagement;

namespace UnityEditor.Multiplayer.Internal
{
    [NativeHeader("Modules/Multiplayer/MultiplayerManager.h")]
    [NativeHeader("Modules/Multiplayer/MultiplayerRolesStripping.h")]
    [StaticAccessor("GetMultiplayerManager()", StaticAccessorType.Dot)]
    internal static partial class EditorMultiplayerManager
    {
        public static extern bool enableMultiplayerRoles { get; set; }
        public static extern MultiplayerRoleFlags activeMultiplayerRoleMask { get; set; }

        public static extern MultiplayerRoleFlags GetMultiplayerRoleMaskForGameObject(GameObject gameObject);
        public static extern MultiplayerRoleFlags GetMultiplayerRoleMaskForComponent(Component component);
        public static extern Type[] GetStrippingTypesForRole(MultiplayerRole role);

        public static extern void SetMultiplayerRoleMaskForGameObject(GameObject gameObject, MultiplayerRoleFlags mask);
        public static extern void SetMultiplayerRoleMaskForComponent(Component component, MultiplayerRoleFlags mask);
        public static extern void SetStrippingTypesForRole(MultiplayerRole role, Type[] types);

        public static extern bool ShouldStripComponentType(MultiplayerRoleFlags activeRoleMask, Component component);

        [StaticAccessor("MultiplayerRolesStripping", StaticAccessorType.DoubleColon)]
        public static extern void StripSceneObjects(MultiplayerRoleFlags activeRoleMask, Scene scene);

        public static extern Hash128 ComputeDependencyHash();


        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        public static event Func<Rect, UnityEngine.Object[], bool> drawingMultiplayerRoleField;
        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        public static event Action<EditorToolbarDropdown> creatingMultiplayerRoleDropdown;
        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        public static event Action activeMultiplayerRoleChanged;
        [AutoStaticsCleanupOnCodeReload] // static event; stale handlers after reload pin old ALC
        public static event Action enableMultiplayerRolesChanged;

        public static event Action<BuildProfile> drawingMultiplayerBuildOptionsForBuildProfile
        {
            add => BuildPlayerWindow.drawingMultiplayerBuildOptions += value;
            remove => BuildPlayerWindow.drawingMultiplayerBuildOptions -= value;
        }

        [EditorHeaderItem(typeof(UnityEngine.Object))]
        private static bool MultiplayerRoleHeaderItem(Rect rect, UnityEngine.Object[] objects)
        {
            if (drawingMultiplayerRoleField == null)
                return false;

            return drawingMultiplayerRoleField(rect, objects);
        }

        public static void CreateMultiplayerRoleDropdown(EditorToolbarDropdown toolbarButton)
        {
            toolbarButton.style.display = DisplayStyle.None;
            creatingMultiplayerRoleDropdown?.Invoke(toolbarButton);
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void InvokeActiveMultiplayerRoleChangeEvent()
        {
            activeMultiplayerRoleChanged?.Invoke();
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void InvokeEnableMultiplayerRolesChangeEvent()
        {
            enableMultiplayerRolesChanged?.Invoke();
        }

        public static string GetUniqueKeyForClassicTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
        {
            var isStandalone = BuildPipeline.GetBuildTargetGroup(buildTarget) == BuildTargetGroup.Standalone;
            var platformGuid = (isStandalone && subtarget == StandaloneBuildSubtarget.Server)
                ? BuildTargetDiscovery.GetGUIDFromBuildTarget(NamedBuildTarget.Server, buildTarget)
                : BuildTargetDiscovery.GetGUIDFromBuildTarget(buildTarget);

            return platformGuid.ToString();
        }

        [RequiredByNativeCode]
        static MultiplayerRoleFlags GetMultiplayerRoleFromCurrentBuildProcess()
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            MultiplayerRoleFlags multiplayerRole;
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

            return multiplayerRole;
        }
    }
}
