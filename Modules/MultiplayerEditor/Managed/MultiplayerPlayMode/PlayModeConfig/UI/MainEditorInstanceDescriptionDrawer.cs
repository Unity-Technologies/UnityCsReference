// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Multiplayer.Internal;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [CustomPropertyDrawer(typeof(MainEditorInstanceDescription))]
    class MainEditorInstanceDescriptionDrawer : PropertyDrawer
    {
        const string k_MultiplayerRoleTooltip = "Indicates the multiplayer role for this instance. The role is determined by the selected build profile: Server build profiles assign the Server role, while Standalone build profiles assign the Client role.";
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            if (EditorMultiplayerManager.enableMultiplayerRoles)
            {
                container.Add(CreateRoleField(property));
            }
            container.Add(CreateTagsField(property));
            container.Add(new PropertyField(property.FindPropertyRelative("m_InitialScene")));
            return container;
        }

        private static VisualElement CreateRoleField(SerializedProperty property)
        {
            property = property.FindPropertyRelative("m_Role");
            var dropdown = new PopupField<MultiplayerRoleFlags>() { label = "Multiplayer Role", name= InstanceDescriptionDrawer.k_MultiplayerRolePopupName };

            dropdown.choices = new((MultiplayerRoleFlags[])Enum.GetValues(typeof(MultiplayerRoleFlags)));
            MultiplayerRoleFlags currentSelected = (MultiplayerRoleFlags)property.enumValueFlag;
            dropdown.SetValueWithoutNotify(currentSelected);
            dropdown.formatSelectedValueCallback = MultiplayerPlayerRoleFlagsText;
            var enumProp = property.Copy();
            dropdown.AddToClassList("unity-base-field__aligned");
            dropdown.tooltip = k_MultiplayerRoleTooltip;

            dropdown.RegisterValueChangedCallback(evt =>
            {
                if (RoleChoiceIsAllowed(evt.newValue, evt.previousValue, enumProp.serializedObject.targetObject as OrchestratedScenario))
                {
                    enumProp.intValue = (int)evt.newValue;
                    enumProp.serializedObject.ApplyModifiedProperties();

                    return;
                }

                dropdown.SetValueWithoutNotify(evt.previousValue);
            });
            dropdown.formatListItemCallback = MultiplayerPlayerRoleFlagsText;
            return dropdown;
        }

        private static string MultiplayerPlayerRoleFlagsText(MultiplayerRoleFlags flag)
        {
            return flag switch
            {
                MultiplayerRoleFlags.ClientAndServer => "Client And Server",
                MultiplayerRoleFlags.Client => "Client",
                MultiplayerRoleFlags.Server => "Server",
                _ => throw new Exception($"Unsupported Multiplayer Player Role Flag: {flag}")
            };
        }

        private static VisualElement CreateTagsField(SerializedProperty property)
        {
            property = property.FindPropertyRelative("m_PlayerTag");
            List<string> choices = new(ProjectDataStore.GetMain().GetAllPlayerTags());
            choices.Insert(0, "None");
            var tagsField = new PopupField<String>() { label = "Tag", choices = choices };
            tagsField.SetValueWithoutNotify(property.stringValue == "" ? "None" : property.stringValue);
            var tagProp = property.Copy();
            tagsField.AddToClassList("unity-base-field__aligned");
            tagsField.tooltip =
                "Currently only one tag is supported per editor instance. To add a tag to the list of tags go to Project Settings->Multiplayer->Playmode, " +
                "then select the tag from the dropdown menu.";
            tagsField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "None")
                {
                    if (evt.newValue != evt.previousValue)
                    {
                        tagProp.stringValue = "";
                        tagProp.serializedObject.ApplyModifiedProperties();
                    }
                    return;
                }
                tagProp.stringValue = evt.newValue;
                tagProp.serializedObject.ApplyModifiedProperties();
            });

            return tagsField;
        }

        static bool RoleChoiceIsAllowed(MultiplayerRoleFlags newRole, MultiplayerRoleFlags oldRole, OrchestratedScenario config)
        {
            // if it was already a server, it's ok
            if (oldRole != MultiplayerRoleFlags.Client)
                return true;

            if (newRole != MultiplayerRoleFlags.Client)
            {
                if (config == null)
                    return false;

                var currentServerCount = config.EditorInstance.RoleMask != MultiplayerRoleFlags.Client ? 1 : 0;
                foreach (var inst in config.VirtualEditorInstances)
                {
                    if (ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client)
                        currentServerCount++;
                }
                foreach (var inst in config.LocalInstances)
                {
                    if (ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client)
                        currentServerCount++;
                }

                if (currentServerCount + 1 > ScenarioConfigEditor.MaxServerCount)
                {
                    EditorUtility.DisplayDialog("Info", $"You can only have {ScenarioConfigEditor.MaxServerCount} server instances", "Ok");
                    return false;
                }
            }

            return true;
        }
    }
}
