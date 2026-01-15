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
        InspectorElement m_Inspector;
        IVisualElementScheduledItem m_RepaintScheduler;
        long m_LastInteractionTime;
        const long k_RepaintDurationMs = 700;

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

            // Listen for user interactions to detect when IMGUI animations might occur.
            // We repaint temporarily after interactions since IMGUI doesn't communicate repaint needs to UI Toolkit.
            RegisterCallback<MouseDownEvent>(OnUserInteraction, TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(OnUserInteraction, TrickleDown.TrickleDown);
            RegisterCallback<ChangeEvent<bool>>(OnUserInteraction, TrickleDown.TrickleDown);

            // Start repaint scheduler
            m_RepaintScheduler = schedule.Execute(RepaintIfNeeded).Every(16); // every 16ms roughly equals to 60fps
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<MouseDownEvent>(OnUserInteraction, TrickleDown.TrickleDown);
            UnregisterCallback<KeyDownEvent>(OnUserInteraction, TrickleDown.TrickleDown);
            UnregisterCallback<ChangeEvent<bool>>(OnUserInteraction, TrickleDown.TrickleDown);

            m_RepaintScheduler?.Pause();
            m_RepaintScheduler = null;

            if (m_PlayerSettingsEditor != null)
                UnityEngine.Object.DestroyImmediate(m_PlayerSettingsEditor);

            m_PlayerSettingsEditor = null;
            m_Inspector = null;
            this.Clear();
        }

        void OnUserInteraction<T>(T evt) where T : EventBase
        {
            m_LastInteractionTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        void RepaintIfNeeded()
        {
            // Only repaint if we're within the window after last interaction
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long timeSinceInteraction = currentTime - m_LastInteractionTime;

            if (timeSinceInteraction < k_RepaintDurationMs)
            {
                m_Inspector?.MarkDirtyRepaint();
            }
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

            m_Inspector = new InspectorElement(m_PlayerSettingsEditor);
            m_Inspector.style.flexGrow = 1;
            m_Inspector.TrackSerializedObjectValue(m_PlayerSettingsEditor.serializedObject, OnPlayerSettingsEditorChanged);

            this.Add(m_Inspector);
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
