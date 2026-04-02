// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using System.Collections.Generic;
using System;
using UnityEditor.Multiplayer.Internal;
using UnityEditor;

using InternalManager = UnityEditor.Multiplayer.Internal.EditorMultiplayerManager;
using Object = UnityEngine.Object;


namespace Unity.Multiplayer.Editor
{
    /// <summary>
    /// Provides an api for managing multiplayer roles in the editor.
    /// </summary>
    public static class EditorMultiplayerRolesManager
    {
        /// <summary>
        /// Enables multiplayer roles for the project.
        /// </summary>
        public static bool EnableMultiplayerRoles
        {
            get => InternalManager.enableMultiplayerRoles;
            set => InternalManager.enableMultiplayerRoles = value;
        }

        /// <summary>
        /// Enables safety checks for multiplayer roles.
        /// When entering play mode or building scenes, the editor will check and warn about any stripped GameObject or Component that is
        /// referenced by other objects and that can potentially cause null reference errors.
        /// </summary>
        /// <remarks>
        /// Disabling this option could improve the performance of entering play mode or building scenes.
        /// </remarks>
        public static bool EnableSafetyChecks
        {
            get => ContentSelectionSettings.EnableSafetyChecks;
            set => ContentSelectionSettings.EnableSafetyChecks = value;
        }

        /// <summary>
        /// Gets or sets the active multiplayer role mask.
        /// </summary>
        public static MultiplayerRoleFlags ActiveMultiplayerRoleMask
        {
            get => (MultiplayerRoleFlags)InternalManager.activeMultiplayerRoleMask;
            set
            {
                if (value == 0 || (value & ~MultiplayerRoleFlags.ClientAndServer) != 0)
                    throw new ArgumentException($"Invalid multiplayer role mask value ({value}).");

                InternalManager.activeMultiplayerRoleMask = value;
            }
        }

        /// <summary>
        /// Event that is invoked when the active multiplayer role mask changes.
        /// </summary>
        public static event Action ActiveMultiplayerRoleChanged
        {
            add => InternalManager.activeMultiplayerRoleChanged += value;
            remove => InternalManager.activeMultiplayerRoleChanged -= value;
        }

        /// <summary>
        /// Event that is invoked when the enable multiplayer roles option changes.
        /// </summary>
        internal static event Action EnableMultiplayerRolesChanged
        {
            add => InternalManager.enableMultiplayerRolesChanged += value;
            remove => InternalManager.enableMultiplayerRolesChanged -= value;
        }

        private static void NotNullArgumentOrThrow(Object obj, string name)
        {
            if (obj == null)
                throw new System.ArgumentNullException(name);
        }

        /// <summary>
        /// Gets the multiplayer role mask for a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject.</param>
        /// <returns>Returns the multiplayer role mask for the provided GameObject.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleMaskForGameObject(GameObject gameObject)
        {
            NotNullArgumentOrThrow(gameObject, nameof(gameObject));
            return (MultiplayerRoleFlags)InternalManager.GetMultiplayerRoleMaskForGameObject(gameObject);
        }

        /// <summary>
        /// Gets the multiplayer role mask for a Component.
        /// </summary>
        /// <param name="component">The Component.</param>
        /// <returns>Returns the multiplayer role mask for the provided Component.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleMaskForComponent(Component component)
        {
            NotNullArgumentOrThrow(component, nameof(component));
            return (MultiplayerRoleFlags)InternalManager.GetMultiplayerRoleMaskForComponent(component);
        }

        /// <summary>
        /// Sets the multiplayer role mask for a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to set the multiplayer role mask to.</param>
        /// <param name="mask">The multiplayer role mask to assing to the GameObject.</param>
        public static void SetMultiplayerRoleMaskForGameObject(GameObject gameObject, MultiplayerRoleFlags mask)
        {
            NotNullArgumentOrThrow(gameObject, nameof(gameObject));
            InternalManager.SetMultiplayerRoleMaskForGameObject(gameObject, mask);
        }

        /// <summary>
        /// Sets the multiplayer role mask for a Component.
        /// </summary>
        /// <param name="component">The Component to set the multiplayer role mask to.</param>
        /// <param name="mask">The multiplayer role mask to assing to the Component.</param>
        public static void SetMultiplayerRoleMaskForComponent(Component component, MultiplayerRoleFlags mask)
        {
            NotNullArgumentOrThrow(component, nameof(component));

            // do nothing if component type has the restricted attribute
            if (component.GetType().IsDefined(typeof(MultiplayerRoleRestrictedAttribute), true))
            {
                Debug.LogWarning($"Cannot set the multiplayer role for the component {component.GetType().Name} because it has the {nameof(MultiplayerRoleRestrictedAttribute)} attribute.");
                return;
            }

            InternalManager.SetMultiplayerRoleMaskForComponent(component, mask);
        }

        /// <summary>
        /// Gets the multiplayer role mask that is going to be used for the provided build target.
        /// </summary>
        /// <param name="namedBuildTarget">The build target to get the multiplayer role mask for.</param>
        /// <returns>Returns the multiplayer role mask for the provided build target.</returns>
        /// <remarks>
        /// For compatibility with build profiles use GetMultiplayerRoleForBuildProfile instead.
        /// </remarks>
        [Obsolete("Use GetMultiplayerRoleForBuildProfile or GetMultiplayerRoleForClassicTarget instead.", false)]
        public static MultiplayerRoleFlags GetMultiplayerRoleForBuildTarget(NamedBuildTarget namedBuildTarget)
            => throw new NotSupportedException("Use GetMultiplayerRoleForBuildProfile or GetMultiplayerRoleForClassicTarget instead.");

        /// <summary>
        /// Gets the multiplayer role that is going to be used for the provided build target.
        /// </summary>
        /// <param name="buildTarget">The build target to get the multiplayer role mask for.</param>
        /// <returns>Returns the multiplayer role mask for the provided build target.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleForClassicTarget(BuildTarget buildTarget)
            => GetMultiplayerRoleForClassicTarget(buildTarget, StandaloneBuildSubtarget.Default);

        /// <summary>
        /// Gets the multiplayer role that is going to be used for the provided build target and subtarget.
        /// </summary>
        /// <param name="buildTarget">The build target to get the multiplayer role mask for.</param>
        /// <param name="subtarget">The subtarget to get the multiplayer role mask for.</param>
        /// <returns>Returns the multiplayer role mask for the provided build target and subtarget.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleForClassicTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget)
            => (MultiplayerRoleFlags)MultiplayerRolesSettings.instance.GetMultiplayerRoleForClassicTarget(buildTarget, subtarget);

        /// <summary>
        /// Gets the multiplayer role that is going to be used for the provided build profile.
        /// </summary>
        /// <param name="profile">The build profile to get the multiplayer role maks for.</param>
        /// <returns>Returns the multiplayer role mask for the provided build profile.</returns>
        public static MultiplayerRoleFlags GetMultiplayerRoleForBuildProfile(BuildProfile profile)
            => (MultiplayerRoleFlags)MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(profile);

        /// <summary>
        /// Gets the multiplayer role string that is going to be used for the provided build profile.
        /// </summary>
        /// <param name="profile">The build profile to get the multiplayer role string for.</param>
        /// <returns>Returns the multiplayer role string for the provided build profile.</returns>
        public static string GetMultiplayerRoleStringForBuildProfile(BuildProfile profile)
        {
            return profile == null ? string.Empty : MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(profile).ToString();
        }

        /// <summary>
        /// Sets the multiplayer role mask that is going to be used for the provided build target.
        /// </summary>
        /// <param name="namedBuildTarget">The build target to set the multiplayer role mask for.</param>
        /// <param name="mask">The multiplayer role mask to assing to the build target.</param>
        /// <remarks>
        /// For compatibility with build profiles use SetMultiplayerRoleForBuildProfile instead.
        /// </remarks>
        [Obsolete("Use SetMultiplayerRoleForBuildProfile or SetMultiplayerRoleForClassicTarget instead.", false)]
        public static void SetMultiplayerRoleForBuildTarget(NamedBuildTarget namedBuildTarget, MultiplayerRoleFlags mask)
            => throw new NotSupportedException("Use SetMultiplayerRoleForBuildProfile or SetMultiplayerRoleForClassicTarget instead.");

        /// <summary>
        /// Sets the multiplayer role that is going to be used for the provided build target.
        /// </summary>
        /// <param name="buildTarget">The build target to set the multiplayer role mask for.</param>
        /// <param name="mask">The multiplayer role mask to assing to the build target.</param>
        public static void SetMultiplayerRoleForClassicTarget(BuildTarget buildTarget, MultiplayerRoleFlags mask)
            => SetMultiplayerRoleForClassicTarget(buildTarget, StandaloneBuildSubtarget.Default, mask);

        /// <summary>
        /// Sets the multiplayer role that is going to be used for the provided build target and subtarget.
        /// </summary>
        /// <param name="buildTarget">The build target to set the multiplayer role mask for.</param>
        /// <param name="subtarget">The subtarget to set the multiplayer role mask for.</param>
        /// <param name="mask">The multiplayer role mask to assing to the build target and subtarget.</param>
        public static void SetMultiplayerRoleForClassicTarget(BuildTarget buildTarget, StandaloneBuildSubtarget subtarget, MultiplayerRoleFlags mask)
            => MultiplayerRolesSettings.instance.SetMultiplayerRoleForClassicTarget(buildTarget, subtarget, mask);

        /// <summary>
        /// Sets the multiplayer role mask that is going to be used for the provided build profile.
        /// </summary>
        /// <param name="profile">The build profile to set the multiplayer role mask for.</param>
        /// <param name="mask">The multiplayer role mask to assing to the build profile.</param>
        public static void SetMultiplayerRoleForBuildProfile(BuildProfile profile, MultiplayerRoleFlags mask)
            => MultiplayerRolesSettings.instance.SetMultiplayerRoleForBuildProfile(profile, mask);

        internal static bool ShouldStrip(MultiplayerRoleFlags activeMask, GameObject gameObject)
        {
            if (!EnableMultiplayerRoles)
                return false;

            return (activeMask & GetMultiplayerRoleMaskForGameObject(gameObject)) == 0;
        }

        internal static bool ShouldStrip(MultiplayerRoleFlags activeMask, Component component)
        {
            if (!EnableMultiplayerRoles)
                return false;

            return (activeMask & GetMultiplayerRoleMaskForComponent(component)) == 0 ||
                InternalManager.ShouldStripComponentType(activeMask, component);
        }

        /// <summary>
        /// Provides an api for automatically stripping specific component types for the multiplayer roles.
        /// </summary>
        public static class AutomaticSelection
        {
            /// <summary>
            /// Provides an api for automatically stripping common component types for the server multiplayer role.
            /// </summary>
            public static class Server
            {
                /// <summary>
                /// Strips common rendering components for the server multiplayer role.
                /// </summary>
                public static bool StripRenderingComponents
                {
                    get => ContentSelectionSettings.AutomaticSelection.StripRenderingComponents;
                    set => ContentSelectionSettings.AutomaticSelection.StripRenderingComponents = value;
                }

                /// <summary>
                /// Strips common UI components for the server multiplayer role.
                /// </summary>
                public static bool StripUIComponents
                {
                    get => ContentSelectionSettings.AutomaticSelection.StripUIComponents;
                    set => ContentSelectionSettings.AutomaticSelection.StripUIComponents = value;
                }

                /// <summary>
                /// Strips common audio components for the server multiplayer role.
                /// </summary>
                public static bool StripAudioComponents
                {
                    get => ContentSelectionSettings.AutomaticSelection.StripAudioComponents;
                    set => ContentSelectionSettings.AutomaticSelection.StripAudioComponents = value;
                }
            }

            /// <summary>
            /// Gets the list of custom components that are automatically selected for the entire project.
            /// </summary>
            /// <returns>Returns a Dictionary where the key is the component Type and the value is the MultiplayerRoleFlags.</returns>
            public static Dictionary<Type, MultiplayerRoleFlags> GetCustomComponents()
                => ContentSelectionSettings.AutomaticSelection.GetCustomComponents();

            /// <summary>
            /// Sets the list of custom components that are automatically selected for the entire project.
            /// </summary>
            /// <param name="customComponents">A Dictionary where the key is the component Type and the value is the MultiplayerRoleFlags.</param>
            public static void SetCustomComponents(Dictionary<Type, MultiplayerRoleFlags> customComponents)
                => ContentSelectionSettings.AutomaticSelection.SetCustomComponents(customComponents);

            /// <summary>
            /// Gets the multiplayer role mask for a component type.
            /// </summary>
            /// <param name="type">The component type.</param>
            /// <returns>Returns the multiplayer role mask of the component type.</returns>
            public static MultiplayerRoleFlags GetMultiplayerRoleMaskForComponentType(Type type)
                => ContentSelectionSettings.AutomaticSelection.GetMultiplayerRoleMaskForComponentType(type);

            /// <summary>
            /// Sets the multiplayer role mask for a component type.
            /// </summary>
            /// <param name="type">The component type.</param>
            /// <param name="mask">The multiplayer role mask to assing to the component type.</param>
            public static void SetMultiplayerRoleMaskForComponentType(Type type, MultiplayerRoleFlags mask)
                => ContentSelectionSettings.AutomaticSelection.SetMultiplayerRoleMaskForComponentType(type, mask);
        }
    }
}
