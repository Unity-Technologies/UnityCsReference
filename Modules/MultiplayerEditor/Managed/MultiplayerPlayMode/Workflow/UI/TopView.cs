// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
using UnityEditor.Multiplayer.Internal;
using UnityEngine.Multiplayer.Internal;

using PopupWindow = UnityEditor.PopupWindow;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class TopViewPermanence
    {
        public const string k_EnableMultiplayerRoles = nameof(k_EnableMultiplayerRoles);

        public static readonly bool Initialized; // Note: the constructor won't run without some data being in the static class
        public static Action EnableMultiplayerRolesEvent;

        static TopViewPermanence()
        {
            Initialized = true;
            SessionState.SetBool(k_EnableMultiplayerRoles, EditorMultiplayerManager.enableMultiplayerRoles);
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(k_EnableMultiplayerRoles, false) != EditorMultiplayerManager.enableMultiplayerRoles)
                {
                    SessionState.SetBool(k_EnableMultiplayerRoles, EditorMultiplayerManager.enableMultiplayerRoles);
                    EnableMultiplayerRolesEvent?.Invoke();
                }
            };
        }
    }

    class TopView : EditorWindow
    {
        public static int NumberOfTopViews;
        static MultiplayerRolesToolbarExtensions.MultiplayerRolesToolbarDropdown s_ToolbarButton;

        public void OnDestroy()
        {
            NumberOfTopViews--;
        }

        public void CreateGUI()
        {
            Debug.Assert(TopViewPermanence.Initialized, "The TopView did not initialize correctly.");
            NumberOfTopViews++;
            if (NumberOfTopViews > 1)
            {
                Debug.LogWarning("An editor should only have 0 or 1 TopView so something bad happened");
                return;
            }

            var checkboxPopupWindow = new WindowLayoutPopoutWindow();
            var toolbar = new Toolbar();

            AddMultiplayerRoleDropdown(toolbar);
            var layoutDropdown = new EditorToolbarDropdown
            {
                icon = GetLayoutIcon(),
                text = "Layout",
            };
            toolbar.Add(layoutDropdown);

            // Apply styles for the entire TopView to more match the standard editor
            rootVisualElement.AddToClassList("unity-editor-toolbar-container");
            toolbar.AddToClassList("unity-editor-toolbar-container__zone");
            toolbar.style.height = 30f;
            toolbar.style.borderBottomWidth = 0f;
            LoadStyleSheets("MainToolbar", rootVisualElement);
            LoadStyleSheets("EditorToolbar", toolbar);

            rootVisualElement.Add(toolbar);
            rootVisualElement.style.flexDirection = FlexDirection.RowReverse;
            rootVisualElement.style.maxHeight = 30f;

            TopViewPermanence.EnableMultiplayerRolesEvent += () =>
            {
                if (s_ToolbarButton != null)
                {
                    // Update the active appearance
                    s_ToolbarButton.EditorToolbarDropdown.style.display = EditorMultiplayerManager.enableMultiplayerRoles
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            };
            EditorMultiplayerManager.enableMultiplayerRolesChanged += () =>
            {
                if (s_ToolbarButton != null)
                {
                    // Update the active appearance
                    s_ToolbarButton.EditorToolbarDropdown.style.display = EditorMultiplayerManager.enableMultiplayerRoles
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            };
            EditorMultiplayerManager.activeMultiplayerRoleChanged += () =>
            {
                if (s_ToolbarButton != null)
                {
                    s_ToolbarButton.EditorToolbarDropdown.icon = IconPerRole(EditorMultiplayerManager.activeMultiplayerRoleMask);
                }
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (s_ToolbarButton != null)
                {
                    s_ToolbarButton.EditorToolbarDropdown.SetEnabled(state != PlayModeStateChange.EnteredPlayMode && !VirtualProjectsEditor.IsScenarioClone);
                }
                checkboxPopupWindow.OnPlayModeStateChanged(state);
            };
            layoutDropdown.RegisterCallback<ClickEvent>(_ =>
            {
                PopupWindow.Show(layoutDropdown.worldBound, checkboxPopupWindow);
            });

            EditorApplication.update += () =>
            {
                Debug.Assert(TopView.NumberOfTopViews == 0 || TopView.NumberOfTopViews == 1, $"An editor should only have 0 or 1 TopView. [{TopView.NumberOfTopViews}]");
                checkboxPopupWindow.OnUpdate();
            };
        }

        static Texture2D GetLayoutIcon()
        {
            var layoutIconPath = EditorGUIUtility.isProSkin ? $"{UXMLPaths.IconsRoot}/d_layout@2x.png" : $"{UXMLPaths.IconsRoot}/layout@2x.png";
            var texture = EditorGUIUtility.LoadRequired(layoutIconPath) as Texture2D;
            return new GUIContent("Layout", texture).image as Texture2D;
        }

        static void LoadStyleSheets(string name, VisualElement target)
        {
            const string k_StyleSheetsPath = "StyleSheets/Toolbars/";

            var path = k_StyleSheetsPath + name;

            var common = EditorGUIUtility.Load($"{path}Common.uss") as StyleSheet;
            if (common != null)
                target.styleSheets.Add(common);

            var themeSpecificName = EditorGUIUtility.isProSkin ? "Dark" : "Light";
            var themeSpecific = EditorGUIUtility.Load($"{path}{themeSpecificName}.uss") as StyleSheet;
            if (themeSpecific != null)
                target.styleSheets.Add(themeSpecific);
        }

        static void OnDropDownMenuClick(MultiplayerRoleFlags dropdownEntryRole)
        {
            // Update the API
            EditorMultiplayerManager.activeMultiplayerRoleMask = dropdownEntryRole;
            // Update the file on disk that tracks each players current role
            var dataStore = SystemDataStore.GetClone(); // get latest data store
            foreach (var playerStateJson in UnityPlayer.GetPlayers(dataStore))
            {
                if (playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier == VirtualProjectsEditor.CloneIdentifier)
                {
                    Debug.Assert(playerStateJson.Type == PlayerType.Clone);
                    playerStateJson.MultiplayerRole = (int)dropdownEntryRole;
                    dataStore.SavePlayerJson(playerStateJson.Index, playerStateJson);  // updates file on disk
                    break;
                }
            }
        }

        static bool OnIsCheckMarked(MultiplayerRoleFlags role)
        {
            return role == EditorMultiplayerManager.activeMultiplayerRoleMask;
        }

        static Texture2D IconPerRole(MultiplayerRoleFlags role)
        {
            return EditorGUIUtility.IconContent(GetIconForRoleFlags(role)).image as Texture2D;
        }

        private static string GetIconForRoleFlags(MultiplayerRoleFlags roles)
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

        static void AddMultiplayerRoleDropdown(Toolbar toolbar)
        {
            var dropDownInfo = new MultiplayerRolesToolbarExtensions.DropDownInfo
            {
                MapIconToRole = IconPerRole,
                OnIsCheckMarked = OnIsCheckMarked,
                OnHandleClick = OnDropDownMenuClick,
            };
            var initialRole = EditorMultiplayerManager.activeMultiplayerRoleMask;
            s_ToolbarButton = MultiplayerRolesToolbarExtensions.CreateDropdown(initialRole, dropDownInfo);
            // Update the active appearance
            s_ToolbarButton.EditorToolbarDropdown.style.display = EditorMultiplayerManager.enableMultiplayerRoles
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            s_ToolbarButton.EditorToolbarDropdown.SetEnabled(!VirtualProjectsEditor.IsScenarioClone);
            // Add to the toolbar
            toolbar.Add(s_ToolbarButton.EditorToolbarDropdown);

            // Note: Clicking the buttons will update the role and the new value is read in
            // the method StandardCloneWorkflow::Initialize()
        }
    }
}
