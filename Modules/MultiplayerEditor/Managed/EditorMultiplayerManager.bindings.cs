// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Multiplayer.Internal
{
    [NativeHeader("Modules/Multiplayer/MultiplayerManager.h")]
    [StaticAccessor("GetMultiplayerManager()", StaticAccessorType.Dot)]
    internal static class EditorMultiplayerManager
    {
        public static extern bool enableMultiplayerRoles { get; set; }
        public static extern MultiplayerRole activeMultiplayerRole { get; set; }

        public static extern MultiplayerRoleFlags GetMultiplayerRoleMaskForGameObject(GameObject gameObject);
        public static extern MultiplayerRoleFlags GetMultiplayerRoleMaskForComponent(Component component);
        public static extern Type[] GetStrippingTypesForRole(MultiplayerRole role);

        public static extern void SetMultiplayerRoleMaskForGameObject(GameObject gameObject, MultiplayerRoleFlags mask);
        public static extern void SetMultiplayerRoleMaskForComponent(Component component, MultiplayerRoleFlags mask);
        public static extern void SetStrippingTypesForRole(MultiplayerRole role, Type[] types);

        public static event Func<Rect, UnityEngine.Object[], bool> drawingMultiplayerRoleField;
        public static event Action<ToolbarButton> creatingMultiplayerRoleDropdown;
        public static event Action activeMultiplayerRoleChanged;
        public static event Action enableMultiplayerRolesChanged;

        [EditorHeaderItem(typeof(UnityEngine.Object))]
        private static bool MultiplayerRoleHeaderItem(Rect rect, UnityEngine.Object[] objects)
        {
            if (drawingMultiplayerRoleField == null)
                return false;

            return drawingMultiplayerRoleField(rect, objects);
        }

        public static void CreateMultiplayerRoleDropdown(ToolbarButton toolbarButton)
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
    }
}
