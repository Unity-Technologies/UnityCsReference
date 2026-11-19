// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The header and the inspector of a UI selection object, built once and pointed at one selection object after
/// another of the same type.
/// </summary>
/// <remarks>
/// The selection editors show one for their target. A pane that shows one selection at a time keeps one per type
/// and only switches its <see cref="Target"/>, which is much cheaper than building an editor for every selection.
/// Without a target, the content shows nothing and listens to nothing.
/// </remarks>
abstract class UISelectionInspectorContent : IDisposable
{
    UISelectionObject m_Target;
    bool m_Listening;
    bool m_Disposed;

    // The recording start and stop events don't fire when AnimationMode enters plain preview, so this is also
    // polled every editor update to refresh the inspector on those transitions.
    bool m_LastInAnimationMode;

    public abstract VisualElement Header { get; }

    public abstract UIInspector Inspector { get; }

    /// <summary>
    /// The selection object on display, or null for none.
    /// </summary>
    public UISelectionObject Target
    {
        get => m_Target;
        set
        {
            // By reference: a destroyed target compares equal to null, and still has to be let go of.
            if (ReferenceEquals(m_Target, value) || m_Disposed)
                return;

            var previous = m_Target;
            m_Target = value;
            SetListening(!ReferenceEquals(m_Target, null));
            OnTargetChanged(previous);
            ApplyState();
        }
    }

    /// <summary>
    /// The context of the target: where the inspector reads its document and sends its messages.
    /// </summary>
    protected AuthoringContext Context => AuthoringContext.For(m_Target);

    /// <summary>
    /// Points the header and the inspector at the new <see cref="Target"/>, which may be null.
    /// </summary>
    protected abstract void OnTargetChanged(UISelectionObject previous);

    /// <summary>
    /// The recording context of the target, or null when not recording or there is no target.
    /// </summary>
    protected abstract StyleInspectorAnimationRecordingContext CreateRecordingContext();

    /// <summary>
    /// Pushes the recording state of the target onto the inspector. Overrides also apply target-specific state
    /// (edit flags, header visibility, ...).
    /// </summary>
    protected virtual void ApplyState()
    {
        if (m_Target == null)
            return;

        Inspector.RefreshRecordingState(CreateRecordingContext());
    }

    void SetListening(bool listening)
    {
        if (m_Listening == listening)
            return;

        m_Listening = listening;
        if (listening)
        {
            AnimationMode.onAnimationRecordingStart += ApplyState;
            AnimationMode.onAnimationRecordingStop += ApplyState;
            m_LastInAnimationMode = AnimationMode.InAnimationMode();
            EditorApplication.update += OnEditorUpdate;
            UIStageNavigation.StageSettled += OnStageChanged;
        }
        else
        {
            AnimationMode.onAnimationRecordingStart -= ApplyState;
            AnimationMode.onAnimationRecordingStop -= ApplyState;
            EditorApplication.update -= OnEditorUpdate;
            UIStageNavigation.StageSettled -= OnStageChanged;
        }
    }

    void OnEditorUpdate()
    {
        var inMode = AnimationMode.InAnimationMode();
        if (inMode == m_LastInAnimationMode)
            return;

        m_LastInAnimationMode = inMode;
        ApplyState();
    }

    // Only the global context follows the stage: a local one keeps its document whatever stage is open.
    void OnStageChanged(Stage _)
    {
        if (Context.IsGlobal)
            Inspector.ResetSearch();
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;

        Target = null;
        m_Disposed = true;
        Inspector.Dispose();
    }
}
