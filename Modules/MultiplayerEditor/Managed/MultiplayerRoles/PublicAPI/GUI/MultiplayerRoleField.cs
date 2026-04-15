// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using InternalManager = UnityEditor.Multiplayer.Internal.EditorMultiplayerManager;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Multiplayer.Internal;
using UnityEditor.PackageManager;

namespace Unity.Multiplayer.Editor
{
    static class MultiplayerRoleField
    {
        private const string k_AlwaysStrippedWarning = "This component is always stripped because its multiplayer role is not valid.";
        private const string k_AutomaticSelectedMessage = "This component type is automatically strip for this role.\nSee Project Settings > Multiplayer > Multiplayer Roles.";
        private const string k_PrefabMessage = "Open the prefab for editing support.";

        [InitializeOnLoadMethod]
        private static void Init()
        {
            Events.registeredPackages += Reinitialize;
            if(!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
            {
                return;
            }
            InternalManager.drawingMultiplayerRoleField += MultiplayerRoleHeaderItem;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnGameObjectHeader;
        }

        private static void Reinitialize(PackageRegistrationEventArgs args)
        {
            Events.registeredPackages -= Reinitialize;
            InternalManager.drawingMultiplayerRoleField -= MultiplayerRoleHeaderItem;
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnGameObjectHeader;
            Init();
        }

        private static void OnGameObjectHeader(UnityEditor.Editor editor)
        {
            if (!InternalManager.enableMultiplayerRoles)
                return;

            if (!(editor.target is GameObject target))
                return;

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(target);
            if (prefabStage != null && prefabStage.prefabContentsRoot == target)
                return;

            if (PrefabUtility.IsPartOfPrefabAsset(target))
                return;

            var rect = GUILayoutUtility.GetRect(16f, 16f);
            rect.x = rect.xMax - 18;

            using var disabledScope = new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode);
            DrawGUI(rect, editor.targets);
        }

        private static bool MultiplayerRoleHeaderItem(Rect rect, Object[] objects)
        {
            if (!InternalManager.enableMultiplayerRoles)
                return false;

            using var disabledScope = new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode);
            return DrawGUI(rect, objects);
        }

        public static bool DrawGUI(Rect rect, Object[] objects)
        {
            if (objects == null || objects.Length == 0)
                return false;

            var type = objects[0].GetType();

            if ((!type.IsSubclassOf(typeof(Component)) && type != typeof(GameObject))
                || type.IsSubclassOf(typeof(Transform)) || type == typeof(Transform))
                return false;

            var role = GetMixedValue(objects, out _);
            var automaticRole = ContentSelectionSettings.AutomaticSelection.GetInheritMultiplayerRoleFlagsForType(type);

            if (PrefabUtility.IsPartOfPrefabAsset(objects[0]))
            {
                DrawLocked(rect, k_PrefabMessage, role & automaticRole);
                return true;
            }

            Draw(rect, objects, role, automaticRole);
            return true;
        }

        private static MultiplayerRoleFlags GetMultiplayerRoleFlagsForObject(Object obj)
        {
            if (obj != null)
            {
                if (obj is Component component)
                    return EditorMultiplayerRolesManager.GetMultiplayerRoleMaskForComponent(component);

                if (obj is GameObject gameObject)
                    return EditorMultiplayerRolesManager.GetMultiplayerRoleMaskForGameObject(gameObject);
            }

            return MultiplayerRoleFlags.ClientAndServer;
        }

        private static MultiplayerRoleFlags GetMixedValue(Object[] objects, out bool mixed)
        {
            mixed = false;
            var role = GetMultiplayerRoleFlagsForObject(objects[0]);

            if (objects.Length > 1)
            {
                for (int i = 1; i < objects.Length; i++)
                {
                    var obj = objects[i];

                    if (obj == null)
                        continue;

                    var objRole = GetMultiplayerRoleFlagsForObject(obj);
                    if (role != objRole)
                    {
                        mixed = true;
                        return (MultiplayerRoleFlags)0;
                    }
                }
            }

            return role;
        }

        private static void DrawLocked(Rect rect, string tooltip, MultiplayerRoleFlags role = MultiplayerRoleFlags.ClientAndServer)
        {
            using var disabledScope = new EditorGUI.DisabledScope(true);

            var style = EditorStyles.iconButton;
            var content = EditorGUIUtility.IconContent(GetIconForRoleFlags(role));
            content.tooltip = tooltip;

            GUI.Button(rect, content, style);
        }

        private static void Draw(Rect rect, UnityEngine.Object[] objects, MultiplayerRoleFlags role, MultiplayerRoleFlags validRoles = MultiplayerRoleFlags.ClientAndServer)
        {
            var style = EditorStyles.iconButton;
            var content = EditorGUIUtility.IconContent(GetIconForRoleFlags(role & validRoles));
            content.tooltip = (role & validRoles) == 0 ? k_AlwaysStrippedWarning : "Select the Multiplayer Role for this object";

            if (GUI.Button(rect, content, style))
                DrawContextMenu(rect, objects, role, validRoles);
        }

        private static void DrawContextMenu(Rect rect, UnityEngine.Object[] objects, MultiplayerRoleFlags role, MultiplayerRoleFlags validRoles = MultiplayerRoleFlags.ClientAndServer)
        {
            var menu = new GenericMenu();

            rect = new Rect(rect.xMin, rect.yMin, 16f, 16f);

            if (!validRoles.HasFlag(MultiplayerRoleFlags.Client))
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Client", k_AutomaticSelectedMessage), false);
            }
            else
            {
                menu.AddItem(new GUIContent("Client"), role.HasFlag(MultiplayerRoleFlags.Client), () =>
                {
                    role = ToggleRole(role, MultiplayerRoleFlags.Client);
                    ApplyRoleToObjects(objects, role);
                    // DrawContextMenu(rect, objects, role, validRoles);
                });
            }

            if (!validRoles.HasFlag(MultiplayerRoleFlags.Server))
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Server", k_AutomaticSelectedMessage), false);
            }
            else
            {
                menu.AddItem(new GUIContent("Server"), role.HasFlag(MultiplayerRoleFlags.Server), () =>
                {
                    role = ToggleRole(role, MultiplayerRoleFlags.Server);
                    ApplyRoleToObjects(objects, role);
                    // DrawContextMenu(rect, objects, role, validRoles);
                });
            }

            menu.DropDown(rect);
        }

        internal static string GetIconForRoleFlags(MultiplayerRoleFlags roles)
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

        private static void ApplyRoleToObjects(Object[] objects, MultiplayerRoleFlags role)
        {
            foreach (var obj in objects)
            {
                SetMultiplayerRoleValueRecordUndo(obj, role);
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        internal static void SetMultiplayerRoleValueRecordUndo(Object obj, MultiplayerRoleFlags roles)
        {
            Undo.RegisterFullObjectHierarchyUndo(obj, "Set MultiplayerRole for object");

            if (obj is GameObject gameObject)
                EditorMultiplayerRolesManager.SetMultiplayerRoleMaskForGameObject(gameObject, roles);
            else if (obj is Component component)
                EditorMultiplayerRolesManager.SetMultiplayerRoleMaskForComponent(component, roles);

            // EditorUtility.SetDirty(obj);
        }

        private static MultiplayerRoleFlags ToggleRole(MultiplayerRoleFlags current, MultiplayerRoleFlags toggle)
        {
            var newRole = (current & ~toggle) | (toggle & ~current);

            if (newRole == 0)
                newRole = MultiplayerRoleFlags.ClientAndServer & ~toggle;

            return newRole;
        }
    }
}
