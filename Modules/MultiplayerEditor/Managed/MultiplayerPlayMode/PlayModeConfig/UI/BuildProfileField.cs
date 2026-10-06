// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class BuildProfileField : VisualElement
{
    const string k_RoleLabel = "Multiplayer Role";
    const string k_ProfileLabel = "Build Profile";
    const string k_RoleTooltip = "The multiplayer role can be modified in the build profile window or from the build profile asset.";
    const string k_MultipleServersErrorTooltip = "The scenario can only contain one server instance.";
    const string k_OpenBuildProfilesButtonText = "Open Build Profiles";
    const string k_RoleWarningIconClass = "unity-instance-role-icon__warning";
    const string k_ProfileWarningIconClass = "unity-instance-profile-icon__warning";

    TextField m_RoleField;
    FieldIcon<Object> m_WarningIcon;

    public BuildProfileField(SerializedProperty property)
    {
        var profileField = new ObjectField(k_ProfileLabel)
        {
            objectType = typeof(BuildProfile),
            allowSceneObjects = false
        };
        profileField.BindProperty(property);
        profileField.Bind(property.serializedObject);
        profileField.AddToClassList("unity-base-field__aligned");
        // Make the profileField layout independent of the label’s text length, preventing long build profile names from squeezing other UI elements
        profileField.Q(className: "unity-object-field__object").style.flexBasis = 0;

        var profileContainer = new VisualElement();
        var openWindowButton = new Button(OpenBuildProfilesWindow)
        {
            text = k_OpenBuildProfilesButtonText
        };
        m_WarningIcon = new FieldIcon<Object>(profileField, Icons.ImageName.Warning);
        m_WarningIcon.AddToClassList(k_ProfileWarningIconClass);
        profileContainer.Add(profileField);
        profileContainer.Add(openWindowButton);
        Add(profileContainer);

        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            m_RoleField = new TextField(k_RoleLabel);
            m_RoleField.tooltip = k_RoleTooltip;
            m_RoleField.SetEnabled(false);
            m_RoleField.AddToClassList("unity-base-field__aligned");
            var roleIcon = new FieldIcon<string>(m_RoleField, Icons.ImageName.Warning)
            {
                tooltip = k_MultipleServersErrorTooltip
            };
            roleIcon.AddToClassList(k_RoleWarningIconClass);
            Add(m_RoleField);
        }

        profileField.TrackPropertyValue(property, OnProfileChanged);
        OnProfileChanged(property);
    }

    void OnProfileChanged(SerializedProperty property)
    {
        var profile = property.objectReferenceValue as BuildProfile;

        if (!IsBuildProfileSupported(profile, out var reason))
        {
            m_WarningIcon.tooltip = reason;
            m_WarningIcon.style.display = DisplayStyle.Flex;
        }
        else
        {
            m_WarningIcon.style.display = DisplayStyle.None;
        }

        if (m_RoleField == null)
            return;

        if (profile == null)
        {
            m_RoleField.value = string.Empty;
            m_RoleField.style.display = DisplayStyle.None;
            return;
        }

        m_RoleField.style.display = DisplayStyle.Flex;
        m_RoleField.value = MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(profile).ToString();
    }

    bool IsBuildProfileSupported(BuildProfile profile, out string reason)
    {
        // sanity check
        if (profile == null)
        {
            reason = "A Build Profile is required.";
            return false;
        }

        // First check if the specified build target is currently available in the Editor
        if (!InternalUtilities.IsBuildProfileSupported(profile))
        {
            reason = "Build Profile is not supported or installed for this type of instance.";
            return false;
        }

        // Ensure that it is also supported for the current RuntimePlatform
        if (!InternalUtilities.BuildProfileCanRunOnCurrentPlatform(profile))
        {
            reason = $"Build Profile is not supported on the current platform: {Application.platform}.";
            return false;
        }

        reason = null;
        return true;
    }

    void OpenBuildProfilesWindow()
    {
        EditorApplication.ExecuteMenuItem("File/Build Profiles");
    }
}
