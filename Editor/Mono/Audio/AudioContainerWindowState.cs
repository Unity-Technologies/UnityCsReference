// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor;

sealed class AudioContainerWindowState
{
    // Used by tests.
    internal const string previewAudioSourceName = "PreviewAudioSource595651";

    AudioRandomContainer m_AudioContainer;
    AudioSource m_PreviewAudioSource;
    SerializedObject m_SerializedObject;
    VisualElement m_ResourceTrackerElement;
    AudioSource m_TrackedSource;

    // Need this flag to track transport state changes immediately, as there could be a
    // one-frame delay to get the correct value from AudioSource.isContainerPlaying.
    bool m_IsPreviewPlayingOrPausedLocalFlag;
    bool m_IsSuspended;

    internal event EventHandler TargetChanged;
    internal event EventHandler TransportStateChanged;
    internal event EventHandler EditorPauseStateChanged;

    internal AudioContainerWindowState()
    {
        EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
        EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
        Selection.selectionChanged += OnSelectionChanged;
    }

    internal AudioRandomContainer AudioContainer
    {
        get
        {
            if (m_AudioContainer == null && m_TrackedSource == null)
                UpdateTarget();

            return m_AudioContainer;
        }
    }

    internal SerializedObject SerializedObject
    {
        get
        {
            if (m_AudioContainer != null && (m_SerializedObject == null || m_SerializedObject.targetObject != m_AudioContainer))
                m_SerializedObject = new SerializedObject(m_AudioContainer);

            return m_SerializedObject;
        }
    }

    internal string TargetPath { get; private set; }

    internal void UpdateTargetPath()
    {
        if (m_AudioContainer != null)
        {
            TargetPath = AssetDatabase.GetAssetPath(m_AudioContainer);
        }
        else
        {
            TargetPath = null;
        }
    }

    internal void Reset()
    {
        StopPreview();
        m_AudioContainer = null;
        m_SerializedObject = null;
        m_IsPreviewPlayingOrPausedLocalFlag = false;
        UpdateTargetPath();
    }

    internal VisualElement GetResourceTrackerElement()
    {
        m_ResourceTrackerElement = new VisualElement();
        return m_ResourceTrackerElement;
    }

    internal void OnDestroy()
    {
        StopPreview();

        if (m_PreviewAudioSource != null)
            Object.DestroyImmediate(m_PreviewAudioSource.gameObject);

        EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
        EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    internal void Suspend()
    {
        m_IsSuspended = true;
        StopPreview();

        if (m_PreviewAudioSource != null)
            Object.DestroyImmediate(m_PreviewAudioSource.gameObject);
    }

    internal void Resume()
    {
        m_IsSuspended = false;
        UpdateTarget();
    }

    /// <summary>
    /// Updates the current target based on the currently selected object in the editor.
    /// </summary>
    void UpdateTarget()
    {
        if (m_IsSuspended)
            return;

        AudioRandomContainer newTarget = null;
        AudioSource audioSource = null;
        var selectedObject = Selection.activeObject;

        // The logic below deals with selecting our new ARC target, whatever we set m_AudioContainer to below will be
        // used by AudioContainerWindow to display the ARC if the target is valid or a day0 state if the target is null.
        // If the selection is a GameObject, we always want to swap the target, a user selecting GameObjects in the
        // scene hierarchy should always see what ARC is on a particular object, this includes the scenario of not
        // having an AudioSource and the value of the resource property on an AudioSource being null/not an ARC.
        // If the selected object is not a GameObject, we only swap targets if it is an ARC - meaning if you are
        // selecting objects in the project browser it holds on to the last ARC selected.

        if (selectedObject != null)
        {
            if (selectedObject is GameObject go)
            {
                audioSource = go.GetComponent<AudioSource>();

                if (audioSource != null)
                {
                    newTarget = audioSource.resource as AudioRandomContainer;
                }
            }
            else
            {
                if (selectedObject is AudioRandomContainer container)
                {
                    newTarget = container;
                }
                else
                {
                    newTarget = m_AudioContainer;
                }
            }
        }
        else
        {
            newTarget = m_AudioContainer;
        }

        var targetChanged = m_AudioContainer != newTarget;
        var trackedSourceChanged = m_TrackedSource != audioSource;

        if (!targetChanged && !trackedSourceChanged)
        {
            return;
        }

        Reset();

        m_AudioContainer = newTarget;
        m_TrackedSource = audioSource;

        if (m_AudioContainer != null)
        {
            UpdateTargetPath();
        }

        if (targetChanged)
        {
            TargetChanged?.Invoke(this, EventArgs.Empty);
        }

        if (trackedSourceChanged)
        {
            UpdateResourceTrackerElement();
        }
    }

    void OnResourceChanged(SerializedProperty property)
    {
        var container = property.objectReferenceValue as AudioRandomContainer;

        if (m_AudioContainer == container)
            return;

        Reset();
        m_AudioContainer = container;

        if (m_AudioContainer != null)
        {
            UpdateTargetPath();
        }

        TargetChanged?.Invoke(this, EventArgs.Empty);

        UpdateResourceTrackerElement();
    }

    void UpdateResourceTrackerElement()
    {
        if (m_ResourceTrackerElement != null)
        {
            m_ResourceTrackerElement.Unbind();
        }

        if (m_TrackedSource != null)
        {
            var trackedSourceSO = new SerializedObject(m_TrackedSource);
            var trackedSourceResourceProperty = trackedSourceSO.FindProperty("m_Resource");
            m_ResourceTrackerElement.TrackPropertyValue(trackedSourceResourceProperty, OnResourceChanged);
        }
    }

    internal void PlayPreview()
    {
        var canNotPlay = m_IsSuspended || IsPreviewPlayingOrPaused() || !IsReadyToPlayPreview();

        if (canNotPlay)
            return;

        if (m_PreviewAudioSource == null)
        {
            // Create a hidden game object in the scene with an AudioSource for editor previewing purposes.
            // The preview object is created on play and destroyed on stop.
            // This means that this object is a hidden part of the user's scene during play/pause.
            var gameObject = new GameObject
            {
                name = previewAudioSourceName,
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            m_PreviewAudioSource = gameObject.AddComponent<AudioSource>();
            m_PreviewAudioSource.playOnAwake = false;
        }

        m_PreviewAudioSource.resource = m_AudioContainer;
        m_PreviewAudioSource.Play();
        m_IsPreviewPlayingOrPausedLocalFlag = true;
        TransportStateChanged?.Invoke(this, EventArgs.Empty);
        EditorApplication.update += OnEditorApplicationUpdate;
    }

    internal void StopPreview()
    {
        var canNotStop = m_IsSuspended || !IsPreviewPlayingOrPaused();

        if (canNotStop)
            return;

        m_PreviewAudioSource.Stop();
        m_PreviewAudioSource.resource = null;
        m_IsPreviewPlayingOrPausedLocalFlag = false;
        TransportStateChanged?.Invoke(this, EventArgs.Empty);
        EditorApplication.update -= OnEditorApplicationUpdate;
    }

    internal void OnAudioClipListChanged()
    {
        // We don't support live updates that affect the clip scheduling,
        // so stop the preview and any other audio sources that reference this asset.
        AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
        StopPreview();
    }

    internal void Skip()
    {
        var canNotSkip = m_IsSuspended || !IsPreviewPlayingOrPaused();

        if (canNotSkip)
            return;

        m_PreviewAudioSource.SkipToNextElementIfHasContainer();
    }

    internal bool IsPreviewPlayingOrPaused()
    {
        return m_IsPreviewPlayingOrPausedLocalFlag || (m_PreviewAudioSource != null && m_PreviewAudioSource.isContainerPlaying);
    }

    /// <summary>
    /// Checks if the window has a current target with at least one enabled audio clip assigned.
    /// </summary>
    /// <returns>Whether or not there are valid audio clips to play</returns>
    internal bool IsReadyToPlayPreview()
    {
        if (m_AudioContainer == null)
            return false;

        var elements = m_AudioContainer.elements;

        for (var i = 0; i < elements.Length; ++i)
            if (elements[i] != null && elements[i].audioClip != null && elements[i].enabled)
                return true;

        return false;
    }

    internal ActivePlayable[] GetActivePlayables()
    {
        return IsPreviewPlayingOrPaused() ? m_PreviewAudioSource.containerActivePlayables : null;
    }

    internal float GetMeterValue()
    {
        return m_PreviewAudioSource.GetAudioRandomContainerRuntimeMeterValue();
    }

    internal bool IsDirty()
    {
        return m_AudioContainer != null && EditorUtility.IsDirty(m_AudioContainer);
    }

    void OnEditorApplicationUpdate()
    {
        if (m_PreviewAudioSource != null && m_PreviewAudioSource.isContainerPlaying)
            return;

        m_IsPreviewPlayingOrPausedLocalFlag = false;
        TransportStateChanged?.Invoke(this, EventArgs.Empty);
        EditorApplication.update -= OnEditorApplicationUpdate;
    }

    void OnEditorPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode)
        {
            StopPreview();

            if (m_PreviewAudioSource != null)
            {
                Object.DestroyImmediate(m_PreviewAudioSource.gameObject);
            }
        }
    }

    void OnEditorPauseStateChanged(PauseState state)
    {
        if (m_AudioContainer == null || m_IsSuspended)
        {
            return;
        }

        EditorPauseStateChanged?.Invoke(this, EventArgs.Empty);
    }

    void OnSelectionChanged()
    {
        UpdateTarget();
    }
}
