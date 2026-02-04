// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;

namespace UnityEditor.Multiplayer.Internal
{
    internal static class MultiplayerRoleDropdown
    {
        const string k_ElementPath = "Multiplayer/Multiplayer Role";

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_ElementPath, defaultDockIndex = 5, defaultDockPosition = MainToolbarDockPosition.Right)]
        internal static MainToolbarElement Create()
        {
            return CreateInternal(EditorMultiplayerManager.activeMultiplayerRoleMask, EditorApplication.isPlaying);
        }

        internal static MainToolbarElement CreateInternal(MultiplayerRoleFlags activeRole, bool isPlaying)
        {
            var icon = EditorGUIUtility.IconContent(GetIconForRoleFlags(activeRole)).image as Texture2D;
            var content = new MainToolbarContent(icon);
            var mainToolbarDropdown = new MainToolbarDropdown(content, ShowDropdownMenu);
            mainToolbarDropdown.enabled = !isPlaying;
            return mainToolbarDropdown;
        }

        [InitializeOnLoadMethod]
        static void InitializeCallbacks()
        {
            EditorMultiplayerManager.enableMultiplayerRolesChanged += OnEnableMultiplayerRolesChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                MainToolbar.Refresh(k_ElementPath);
            }
        }

        static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Client"), false, () => ChangeMultiplayerRole(MultiplayerRoleFlags.Client));
            menu.AddItem(new GUIContent("Server"), false, () => ChangeMultiplayerRole(MultiplayerRoleFlags.Server));
            menu.AddItem(new GUIContent("Client and Server"), false, () => ChangeMultiplayerRole(MultiplayerRoleFlags.ClientAndServer));
            menu.DropDown(dropDownRect);
        }

        static void ChangeMultiplayerRole(MultiplayerRoleFlags role)
        {
            EditorMultiplayerManager.activeMultiplayerRoleMask = role;
            MainToolbar.Refresh(k_ElementPath);
        }
        static string GetIconForRoleFlags(MultiplayerRoleFlags roles)
        {
            switch (roles)
            {
                case MultiplayerRoleFlags.Client:
                    return "BuildSettings.Standalone.Small";
                case MultiplayerRoleFlags.Server:
                    return "BuildSettings.DedicatedServer.Small";
                case 0:
                    return "Warning@2x";
                default:
                    return "ServerClient@2x";
            }
        }



        [MainToolbarElementAvailability(k_ElementPath)]
        static bool IsAvailable()
        {
            return EditorMultiplayerManager.enableMultiplayerRoles;
        }

        static void OnEnableMultiplayerRolesChanged()
        {
            MainToolbar.Refresh(k_ElementPath);
        }
    }
}
