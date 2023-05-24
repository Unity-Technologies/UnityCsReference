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
    bool m_IsPlayingOrPaused;
    AudioSource m_PreviewAudioSource;
    SerializedObject m_SerializedObject;
    string m_TargetPath;

    internal event EventHandler OnPlaybackStateChanged;
    internal event EventHandler OnTargetChanged;
    internal event EventHandler OnPauseStateChanged;

    internal AudioContainerWindowState()
    {
        EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
    }

    internal AudioRandomContainer AudioContainer
    {
        get
        {
            if (m_AudioContainer == null) UpdateTarget();

            return m_AudioContainer;
        }
    }

    internal SerializedObject SerializedObject
    {
        get
        {
            if (m_AudioContainer != null && (m_SerializedObject == null || m_SerializedObject.targetObject != m_AudioContainer)) m_SerializedObject = new SerializedObject(m_AudioContainer);

            return m_SerializedObject;
        }
    }

    internal string TargetPath => m_TargetPath;

    bool IsPlayingOrPaused
    {
        get => m_IsPlayingOrPaused;
        set
        {
            m_IsPlayingOrPaused = value;
            OnPlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void Reset()
    {
        m_AudioContainer = null;
        m_SerializedObject = null;
        m_IsPlayingOrPaused = false;
        m_TargetPath = null;

        if (m_PreviewAudioSource != null)
        {
            Stop();
            m_PreviewAudioSource.resource = null;
        }
    }

    internal void OnDestroy()
    {
        Stop();
        EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;

        if (m_PreviewAudioSource != null) Object.DestroyImmediate(m_PreviewAudioSource.gameObject);
    }

    internal void Play()
    {
        CreatePreviewObjects();

        if (!IsReadyToPlay()) return;

        m_PreviewAudioSource.Play();
        IsPlayingOrPaused = true;
        EditorApplication.update += OnEditorApplicationUpdate;
    }

    internal void Stop()
    {
        if (!IsPlaying()) return;

        m_PreviewAudioSource.Stop();
        IsPlayingOrPaused = false;
        EditorApplication.update -= OnEditorApplicationUpdate;
    }

    internal void Skip()
    {
        if (!IsPlaying()) return;
        m_PreviewAudioSource.SkipToNextElementIfHasContainer();
    }

    internal bool IsPlaying()
    {
        return IsPlayingOrPaused || (m_PreviewAudioSource != null && m_PreviewAudioSource.isContainerPlaying);
    }

    internal bool IsReadyToPlay()
    {
        if (m_AudioContainer == null)
        {
            return false;
        }

        CreatePreviewObjects();

        if (m_PreviewAudioSource == null)
        {
            return false;
        }

        var elements = m_AudioContainer.elements;
        var elementCount = m_AudioContainer.elements.Length;

        for (var i = 0; i < elementCount; ++i)
        {
            if (elements[i] != null && elements[i].audioClip != null && elements[i].enabled)
            {
                return true;
            }
        }

        return false;
    }

    internal ActivePlayable[] GetActivePlayables()
    {
        return m_PreviewAudioSource == null ? null : m_PreviewAudioSource.containerActivePlayables;
    }

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
                {
                    newTarget = audioSource.resource as AudioRandomContainer;
                }
            }
            else
            {
                audioClipSelected = selectedObject is AudioClip;
                newTarget = selectedObject as AudioRandomContainer;
            }
        }

        if (!audioClipSelected && (newTarget != null && newTarget != m_AudioContainer))
        {
            if (m_AudioContainer != null)
            {
                Stop();
            }

            Reset();
            m_AudioContainer = newTarget;

            if (m_AudioContainer != null)
            {
                m_TargetPath = AssetDatabase.GetAssetPath(m_AudioContainer);
                CreatePreviewObjects();
            }

            OnTargetChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// This method creates a hidden game object in the scene with an audio source for editor previewing purposes.
    /// The preview object is created with the window and destroyed when the window is closed.
    /// This means that this AudioSource object is a hidden part of the user's scene –
    /// but only in the editor and only while the window is open.
    /// </summary>
    void CreatePreviewObjects()
    {
        if (m_AudioContainer == null) return;

        if (m_PreviewAudioSource != null)
        {
            m_PreviewAudioSource.resource = m_AudioContainer;
            return;
        }

        var audioSourceGO = new GameObject
        {
            name = "PreviewAudioSource595651",
            hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };

        m_PreviewAudioSource = audioSourceGO.AddComponent<AudioSource>();
        m_PreviewAudioSource.playOnAwake = false;
        m_PreviewAudioSource.resource = m_AudioContainer;
    }

    internal bool IsDirty()
    {
        return m_AudioContainer != null && EditorUtility.IsDirty(m_AudioContainer);
    }

    void OnEditorApplicationUpdate()
    {
        if (!IsPlayingOrPaused || !m_PreviewAudioSource.isContainerPlaying)
        {
            IsPlayingOrPaused = false;
            EditorApplication.update -= OnEditorApplicationUpdate;
        }
    }

    void OnEditorPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            Stop();
        }
    }

    internal float GetMeterValue()
    {
        return m_PreviewAudioSource == null ? 0.0f : m_PreviewAudioSource.GetAudioRandomContainerRuntimeMeterValue();
    }

    void OnSelectionChanged()
    {
        UpdateTarget();
    }

    void OnEditorPauseStateChanged(PauseState state)
    {
        OnPauseStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
