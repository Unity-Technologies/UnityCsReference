// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using InternalManager = UnityEditor.Multiplayer.Internal.EditorMultiplayerManager;
using Unity.Multiplayer.Internal;
using UnityEditor.PackageManager;

namespace Unity.Multiplayer.Editor
{
    internal static class ToolbarExtensions
    {
        private static EditorToolbarDropdown s_ToolbarButton;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            Events.registeredPackages += Reinitialize;
            if(!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
            {
                return;
            }
            InternalManager.creatingMultiplayerRoleDropdown += OnCreateMultiplayerRoleDropdown;
            InternalManager.enableMultiplayerRolesChanged += OnEnableMultiplayerRolesChanged;
            InternalManager.activeMultiplayerRoleChanged += UpdateText;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void Reinitialize(PackageRegistrationEventArgs args)
        {
            Events.registeredPackages -= Reinitialize;
            InternalManager.creatingMultiplayerRoleDropdown -= OnCreateMultiplayerRoleDropdown;
            InternalManager.enableMultiplayerRolesChanged -= OnEnableMultiplayerRolesChanged;
            InternalManager.activeMultiplayerRoleChanged -= UpdateText;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Init();
        }

        public static void OnCreateMultiplayerRoleDropdown(EditorToolbarDropdown toolbarButton)
        {
            s_ToolbarButton = toolbarButton;

            toolbarButton.name = "ContentProfile";
            toolbarButton.tooltip = "Active Multiplayer Role used in Editor";
            UpdateText();

            toolbarButton.clicked += OpenProfilesWindow;

            OnEnableMultiplayerRolesChanged();
        }

        private static void OpenProfilesWindow()
        {
            var activeRole = EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Client"), activeRole == MultiplayerRoleFlags.Client, SwitchProfile, MultiplayerRoleFlags.Client);
            menu.AddItem(new GUIContent("Server"), activeRole == MultiplayerRoleFlags.Server, SwitchProfile, MultiplayerRoleFlags.Server);
            menu.AddItem(new GUIContent("Client and Server"), activeRole == MultiplayerRoleFlags.ClientAndServer, SwitchProfile, MultiplayerRoleFlags.ClientAndServer);
            menu.DropDown(s_ToolbarButton.worldBound);
        }

        private static void SwitchProfile(object flags)
        {
            EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask = (MultiplayerRoleFlags)flags;
            UpdateText();
        }

        private static void UpdateText()
        {
            if (s_ToolbarButton == null)
                return;

            // s_ToolbarButton.text = $"{EditorMultiplayerRolesManager.activeMultiplayerRoleMask.ToString()}";
            s_ToolbarButton.icon = EditorGUIUtility.IconContent(MultiplayerRoleField.GetIconForRoleFlags(EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask)).image as Texture2D;
        }

        private static void OnEnableMultiplayerRolesChanged()
        {
            if (s_ToolbarButton == null)
                return;

            s_ToolbarButton.style.display = InternalManager.enableMultiplayerRoles
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (s_ToolbarButton == null)
                return;

            s_ToolbarButton.SetEnabled(state != PlayModeStateChange.EnteredPlayMode);
        }
    }
}
