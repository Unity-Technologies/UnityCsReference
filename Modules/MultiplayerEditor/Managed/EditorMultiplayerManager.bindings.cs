// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

namespace UnityEditor.Multiplayer.Internal
{
    [NativeHeader("Modules/Multiplayer/MultiplayerManager.h")]
    [StaticAccessor("GetMultiplayerManager()", StaticAccessorType.Dot)]
    internal static class EditorMultiplayerManager
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
        public static extern Hash128 ComputeDependencyHash();


        public static event Func<Rect, UnityEngine.Object[], bool> drawingMultiplayerRoleField;
        public static event Action<EditorToolbarDropdown> creatingMultiplayerRoleDropdown;
        public static event Action activeMultiplayerRoleChanged;
        public static event Action enableMultiplayerRolesChanged;

        /// <summary>
        /// Use drawingMultiplayerBuildOptionsForBuildProfile instead, this is kept for backwards compatibility.
        /// </summary>
        public static event Action<NamedBuildTarget> drawingMultiplayerBuildOptions
        {
            add => BuildPlayerWindow.drawingMultiplayerBuildOptions += (profile) => value(NamedBuildTarget.FromTargetAndSubtarget(profile.buildTarget, (int)profile.subtarget));
            remove => BuildPlayerWindow.drawingMultiplayerBuildOptions -= (profile) => value(NamedBuildTarget.FromTargetAndSubtarget(profile.buildTarget, (int)profile.subtarget));
        }
        public static event Action<BuildProfile> drawingMultiplayerBuildOptionsForBuildProfile
        {
            add => BuildPlayerWindow.drawingMultiplayerBuildOptions += value;
            remove => BuildPlayerWindow.drawingMultiplayerBuildOptions -= value;
        }
        public static event Action<VisualElement> creatingPlayModeButtons
        {
            add => UnityEditor.Toolbars.PlayModeButtons.onPlayModeButtonsCreated += value;
            remove => UnityEditor.Toolbars.PlayModeButtons.onPlayModeButtonsCreated -= value;
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
    }
}
