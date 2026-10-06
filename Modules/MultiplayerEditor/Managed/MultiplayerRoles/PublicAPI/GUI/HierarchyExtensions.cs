// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using InternalManager = UnityEditor.Multiplayer.Internal.EditorMultiplayerManager;
using System;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.Multiplayer.Internal;
using UnityEditor.PackageManager;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Editor
{
    internal static class HierarchyExtensions
    {
        private const string k_RoleBadgeName = "MultiplayerRoleBadge";
        private const float k_RoleBadgeSize = 16;

        private static readonly Color32 k_StrippedTint = new Color32(253, 134, 120, 255);
        private static readonly Color32 k_IncludedTint = new Color32(196, 196, 196, 255);

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Events.registeredPackages += Reinitialize;
            if(!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
            {
                return;
            }
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnItemGUI;
            EditorApplication.refreshHierarchy += RefreshRoleBadges;
            HierarchyWindow.BindViewItem += OnBindViewItem;
            InternalManager.enableMultiplayerRolesChanged += OnMultiplayerRolesStateChange;
            InternalManager.activeMultiplayerRoleChanged += OnMultiplayerRolesStateChange;
        }

        private static void Reinitialize(PackageRegistrationEventArgs args)
        {
            Events.registeredPackages -= Reinitialize;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnItemGUI;
            EditorApplication.refreshHierarchy -= RefreshRoleBadges;
            HierarchyWindow.BindViewItem -= OnBindViewItem;
            InternalManager.enableMultiplayerRolesChanged -= OnMultiplayerRolesStateChange;
            InternalManager.activeMultiplayerRoleChanged -= OnMultiplayerRolesStateChange;
            Initialize();
        }

        private static void OnMultiplayerRolesStateChange()
        {
            EditorApplication.RepaintHierarchyWindow();
        }

        // A role change dirties no hierarchy node, so no V2 row rebinds on its own; the repaint every role-editing path already issues is the only signal.
        private static void RefreshRoleBadges()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                var root = window.rootVisualElement;
                if (root == null)
                    continue;

                root.Query<HierarchyViewItem>().ForEach(UpdateRoleBadge);
            }
        }

        private static void OnBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            UpdateRoleBadge(item);
        }

        private static void UpdateRoleBadge(HierarchyViewItem item)
        {
            var badge = item.RightCustomContainer.Q<Image>(k_RoleBadgeName);

            if (!TryGetRoleBadge(item, out var icon, out var tint))
            {
                badge?.RemoveFromHierarchy();
                return;
            }

            if (badge == null)
            {
                badge = new Image() { name = k_RoleBadgeName, pickingMode = PickingMode.Ignore };
                badge.style.width = k_RoleBadgeSize;
                badge.style.height = k_RoleBadgeSize;
                badge.style.flexShrink = 0;
                item.RightCustomContainer.Add(badge);
            }

            badge.image = icon;
            badge.tintColor = tint;
        }

        private static bool TryGetRoleBadge(HierarchyViewItem item, out Texture icon, out Color tint)
        {
            icon = null;
            tint = default;

            if (!InternalManager.enableMultiplayerRoles)
                return false;

            // Rows are pooled, so an item between two bindings has no handler and no GameObject.
            if (item.Handler is not HierarchyGameObjectHandler handler)
                return false;

            var gameObject = handler.GetGameObject(item.Node);

            if (gameObject == null)
                return false;

            var target = EditorMultiplayerRolesManager.GetMultiplayerRoleMaskForGameObject(gameObject);
            var iconName = GetRoleIconName(target);

            if (iconName == null)
                return false;

            icon = EditorGUIUtility.IconContent(iconName).image;
            tint = IsStrippedFromActiveRole(target) ? k_StrippedTint : k_IncludedTint;
            return true;
        }

        private static string GetRoleIconName(MultiplayerRoleFlags target)
        {
            if (target == MultiplayerRoleFlags.Server)
                return "BuildSettings.DedicatedServer On";

            if (target == MultiplayerRoleFlags.Client)
                return "BuildSettings.Standalone On";

            return null;
        }

        private static bool IsStrippedFromActiveRole(MultiplayerRoleFlags target)
            => (EditorMultiplayerRolesManager.ActiveMultiplayerRoleMask & target) == 0;

        private static void OnItemGUI(EntityId instanceId, Rect selectionRect)
        {
            if (!InternalManager.enableMultiplayerRoles)
                return;

            var gameObject = EditorUtility.EntityIdToObject(instanceId) as GameObject;

            if (gameObject == null)
                return;

            var target = EditorMultiplayerRolesManager.GetMultiplayerRoleMaskForGameObject(gameObject);
            var iconName = GetRoleIconName(target);

            if (iconName == null)
                return;

            var backgroundColor = IsStrippedFromActiveRole(target) ? k_StrippedTint : k_IncludedTint;

            var iconRect = selectionRect;
            iconRect.width = iconRect.height = k_RoleBadgeSize;
            iconRect.x = selectionRect.xMax - iconRect.width;

            var type = Type.GetType("Cinemachine.CinemachineBrain,solution");
            if(type != null)
            {
                if (gameObject.TryGetComponent(type, out UnityEngine.Component _))
                {
                    iconRect.x -= iconRect.width + 2;
                }
            }

            var icon = EditorGUIUtility.IconContent(iconName).image;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 1, backgroundColor, 0, 0);
        }
    }
}
