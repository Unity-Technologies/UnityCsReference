// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace UnityEditor;

sealed class AudioContainerWindowState
{
    AudioRandomContainer m_AudioContainer;
    AudioSource m_PreviewAudioSource;
    SerializedObject m_SerializedObject;

    // Need this flag to track transport state changes immediately, as there could be a
    // one-frame delay to get the correct value from AudioSource.isContainerPlaying.
    bool m_IsPlayingOrPausedLocalFlag;

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
            if (m_AudioContainer == null)
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

    internal void Reset()
    {
        Stop();
        m_AudioContainer = null;
        m_SerializedObject = null;
        m_IsPlayingOrPausedLocalFlag = false;
        TargetPath = null;
    }

    internal void OnDestroy()
    {
        Stop();

        if (m_PreviewAudioSource != null)
            Object.DestroyImmediate(m_PreviewAudioSource.gameObject);

        EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
    }

    /// <summary>
    /// Updates the current target based on the currently selected object in the editor.
    /// </summary>
    internal void UpdateTarget()
    {
        AudioRandomContainer newTarget = null;
        var selectedObject = Selection.activeObject;
        var audioClipSelected = false;

        if (selectedObject != null)
        {
            if (selectedObject is GameObject go)
            {
                var audioSource = go.GetComponent<AudioSource>();

                if (audioSource != null)
                    newTarget = audioSource.resource as AudioRandomContainer;
            }
            else
            {
                audioClipSelected = selectedObject is AudioClip;
                newTarget = selectedObject as AudioRandomContainer;
            }
        }

        if (!audioClipSelected && newTarget != null && newTarget != m_AudioContainer)
        {
            if (m_AudioContainer != null)
                Stop();

            Reset();
            m_AudioContainer = newTarget;

            if (m_AudioContainer != null)
                TargetPath = AssetDatabase.GetAssetPath(m_AudioContainer);

            TargetChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void Play()
    {
        if (IsPlayingOrPaused() || !IsReadyToPlay())
            return;

        if (m_PreviewAudioSource == null)
        {
            // Create a hidden game object in the scene with an AudioSource for editor previewing purposes.
            // The preview object is created on play and destroyed on stop.
            // This means that this object is a hidden part of the user's scene during play/pause.
            var gameObject = new GameObject
            {
                name = "PreviewAudioSource595651",

                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            m_PreviewAudioSource = gameObject.AddComponent<AudioSource>();
            m_PreviewAudioSource.playOnAwake = false;
        }

        m_PreviewAudioSource.resource = m_AudioContainer;
        m_PreviewAudioSource.Play();
        m_IsPlayingOrPausedLocalFlag = true;
        TransportStateChanged?.Invoke(this, EventArgs.Empty);
        EditorApplication.update += OnEditorApplicationUpdate;
    }

    internal void Stop()
    {
        if (!IsPlayingOrPaused())
            return;

        m_PreviewAudioSource.Stop();
        m_PreviewAudioSource.resource = null;
        m_IsPlayingOrPausedLocalFlag = false;
        TransportStateChanged?.Invoke(this, EventArgs.Empty);
        EditorApplication.update -= OnEditorApplicationUpdate;
    }

    internal void Skip()
    {
        if (!IsPlayingOrPaused())
            return;

        m_PreviewAudioSource.SkipToNextElementIfHasContainer();
    }

    internal bool IsPlayingOrPaused()
    {
        return m_IsPlayingOrPausedLocalFlag || (m_PreviewAudioSource != null && m_PreviewAudioSource.isContainerPlaying);
    }

    /// <summary>
    /// Checks if the window has a current target with at least one enabled audio clip assigned.
    /// </summary>
    /// <returns>Whether or not there are valid audio clips to play</returns>
    internal bool IsReadyToPlay()
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
        return IsPlayingOrPaused() ? m_PreviewAudioSource.containerActivePlayables : null;
    }

    internal float GetMeterValue()
    {
        return IsPlayingOrPaused() ? m_PreviewAudioSource.GetAudioRandomContainerRuntimeMeterValue() : -80f;
    }

    internal bool IsDirty()
    {
        return m_AudioContainer != null && EditorUtility.IsDirty(m_AudioContainer);
    }

    void OnEditorApplicationUpdate()
    {
        if (m_PreviewAudioSource != null && m_PreviewAudioSource.isContainerPlaying)
            return;

        m_IsPlayingOrPausedLocalFlag = false;
        TransportStateChanged?.Invoke(this, EventArgs.Empty);
        EditorApplication.update -= OnEditorApplicationUpdate;
    }

    void OnEditorPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode)
        {
            Stop();

            if (m_PreviewAudioSource != null)
            {
                Object.DestroyImmediate(m_PreviewAudioSource.gameObject);
            }
        }
    }

    void OnEditorPauseStateChanged(PauseState state)
    {
        EditorPauseStateChanged?.Invoke(this, EventArgs.Empty);
    }

    void OnSelectionChanged()
    {
        UpdateTarget();
    }
}
