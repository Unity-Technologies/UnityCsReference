// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Player Settings Editor created for the <see cref="BuildProfile.playerSettings"/> object.
    /// An InspectorElement displays the underlying editor, both of which are recreated on
    /// AttachToPanelEvent/DetachFromPanelEvent events.
    /// </summary>
    class PlayerSettingsVisualElement : VisualElement
    {
        PlayerSettingsEditor m_PlayerSettingsEditor;
        BuildProfile m_Profile;
        SerializedObject m_ProfileSerializedObject;

        public PlayerSettingsVisualElement(BuildProfile profile, SerializedObject serializedObject)
        {
            if (serializedObject == null)
                throw new ArgumentNullException(nameof(serializedObject));
            if (serializedObject.targetObject is not BuildProfile buildProfile)
                throw new InvalidOperationException("Editor object is not of type BuildProfile.");
            if (!BuildProfileModuleUtil.HasSerializedPlayerSettings(buildProfile))
                throw new ArgumentException("Build Profile does not contain player settings.");

            m_Profile = buildProfile;
            m_ProfileSerializedObject = serializedObject;

            // PlayerSettings Editor and InspectorElement must be recreated on detach/attach to panel
            // events to handle enter play mode and exit play mode correctly as the PlayerSettingsEditor
            // must be manually destroyed.
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Verify player settings object is valid. Remove player settings
            // may destroy the object before the asset is updated on disk.
             if (m_Profile.playerSettings == null)
                return;

            if (m_PlayerSettingsEditor == null)
                ShowEditor();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_PlayerSettingsEditor != null)
                UnityEngine.Object.DestroyImmediate(m_PlayerSettingsEditor);

            m_PlayerSettingsEditor = null;
            this.Clear();
        }

        void ShowEditor()
        {
            bool isActiveProfile = BuildProfile.GetActiveBuildProfile() == m_Profile;
            m_PlayerSettingsEditor = Editor.CreateEditor(m_Profile.playerSettings) as PlayerSettingsEditor;
            m_PlayerSettingsEditor.ConfigurePlayerSettingsForBuildProfile(
                m_ProfileSerializedObject,
                m_Profile.platformGuid,
                isActiveProfile,
                OnPlayerSettingsEditorChanged);

            var inspector = new InspectorElement(m_PlayerSettingsEditor);
            inspector.style.flexGrow = 1;
            inspector.TrackSerializedObjectValue(m_PlayerSettingsEditor.serializedObject, OnPlayerSettingsEditorChanged);

            this.Add(inspector);
        }

        void OnPlayerSettingsEditorChanged(SerializedObject playerSettingsSerializedObject)
        {
            playerSettingsSerializedObject.ApplyModifiedProperties();
            BuildProfileModuleUtil.SerializePlayerSettings(m_Profile);
            m_ProfileSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Profile);
        }
    }
}
