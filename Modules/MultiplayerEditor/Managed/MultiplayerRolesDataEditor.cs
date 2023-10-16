// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditor.Multiplayer.Internal
{
    [CustomEditor(typeof(MultiplayerRolesData), true)]
    internal class MultiplayerRolesDataEditor : Editor
    {
        private SerializedProperty m_GameObjectRoleProperty;
        private SerializedProperty m_ComponentsRoleProperty;
        private readonly Dictionary<Component, SerializedProperty> m_ComponentRoleProperties = new();

        public void OnEnable()
        {
            m_GameObjectRoleProperty = serializedObject.FindProperty("m_GameObjectRolesMask");
            m_ComponentsRoleProperty = serializedObject.FindProperty("m_ComponentsRolesMasks");
            var componentsCount = m_ComponentsRoleProperty.arraySize;
            for (int i = 0; i < componentsCount; i++)
            {
                var componentRole = m_ComponentsRoleProperty.GetArrayElementAtIndex(i);
                var componentProperty = componentRole.FindPropertyRelative("m_Object");
                var roleProperty = componentRole.FindPropertyRelative("m_RolesMask");
                if (componentProperty.objectReferenceValue is Component component)
                {
                    m_ComponentRoleProperties.Add(component, roleProperty);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var gameObjectRole = (MultiplayerRoleFlags)m_GameObjectRoleProperty.intValue;
            var newGameObjectRole = (MultiplayerRoleFlags)EditorGUILayout.EnumPopup("Game Object:", gameObjectRole);
            if (gameObjectRole != newGameObjectRole)
            {
                m_GameObjectRoleProperty.intValue = (int)newGameObjectRole;
            }

            foreach (var kvp in m_ComponentRoleProperties)
            {
                var component = kvp.Key;
                var role = (MultiplayerRoleFlags)kvp.Value.intValue;

                var newRole = (MultiplayerRoleFlags)EditorGUILayout.EnumPopup($"{component.name}:", role);
                if (role != newRole)
                {
                    kvp.Value.intValue = (int)newRole;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
