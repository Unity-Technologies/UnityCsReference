// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using Unity.Multiplayer;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MultiplayerRolesToolbarExtensions
    {
        internal const string k_MultiplayerRoleDropDownName = "ContentProfile";

        public struct DropDownInfo
        {
            public Func<MultiplayerRoleFlags, Texture2D> MapIconToRole;
            public Func<MultiplayerRoleFlags, bool> OnIsCheckMarked;
            public Action<MultiplayerRoleFlags> OnHandleClick;
        }

        public class MultiplayerRolesToolbarDropdown
        {
            public EditorToolbarDropdown EditorToolbarDropdown;

            internal void NewProfilesWindow(DropDownInfo info)
            {
                var menu = new GenericMenu();
                GenericMenu.MenuFunction2 func = flags
                    => info.OnHandleClick((MultiplayerRoleFlags)flags);
                menu.AddItem(new GUIContent("Client"), info.OnIsCheckMarked(MultiplayerRoleFlags.Client), func, MultiplayerRoleFlags.Client);
                menu.AddItem(new GUIContent("Server"), info.OnIsCheckMarked(MultiplayerRoleFlags.Server), func, MultiplayerRoleFlags.Server);
                menu.AddItem(new GUIContent("Client and Server"), info.OnIsCheckMarked(MultiplayerRoleFlags.ClientAndServer), func, MultiplayerRoleFlags.ClientAndServer);
                menu.DropDown(EditorToolbarDropdown.worldBound);
            }
        }

        public static MultiplayerRolesToolbarDropdown CreateDropdown(MultiplayerRoleFlags currentRole, DropDownInfo info)
        {
            var multiplayerRolesToolbarDropdown = new MultiplayerRolesToolbarDropdown
            {
                EditorToolbarDropdown = new EditorToolbarDropdown
                {
                    name = k_MultiplayerRoleDropDownName,
                    tooltip = "Active Multiplayer Role used in Editor",
                    icon = info.MapIconToRole(currentRole),
                },
            };

            multiplayerRolesToolbarDropdown.EditorToolbarDropdown.clicked += () =>
            {
                multiplayerRolesToolbarDropdown.NewProfilesWindow(info);
            };

            return multiplayerRolesToolbarDropdown;
        }
    }
}
