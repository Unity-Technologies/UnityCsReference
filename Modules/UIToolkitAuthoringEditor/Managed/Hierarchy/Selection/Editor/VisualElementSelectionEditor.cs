// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(VisualElementSelection))]
class VisualElementSelectionEditor : UnityEditor.Editor
{
    VisualElementInspector m_Inspector;
    // Tracks AnimationMode preview-mode transitions that do NOT go through the recording
    // start/stop events (e.g. a controller flips previewing=true without recording=true).
    // The animation-driven inspector tint must update on those transitions, otherwise the
    // affordance and field-background class stay stale until the user clicks the field.
    bool m_LastInAnimationMode;

    private VisualElementSelection Target => (VisualElementSelection)target;

    void OnEnable()
    {
        AnimationMode.onAnimationRecordingStart += OnRecordingStateChanged;
        AnimationMode.onAnimationRecordingStop += OnRecordingStateChanged;
        m_LastInAnimationMode = AnimationMode.InAnimationMode();
        EditorApplication.update += OnEditorUpdate;
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
    }

    void OnDisable()
    {
        AnimationMode.onAnimationRecordingStart -= OnRecordingStateChanged;
        AnimationMode.onAnimationRecordingStop -= OnRecordingStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
        Target.propertyChanged -= OnTargetPropertyChanged;
    }

    void OnStageChanged(Stage _) => m_Inspector?.ResetSearch();

    void OnRecordingStateChanged()
    {
        ApplyState();
    }

    void OnEditorUpdate()
    {
        // Single bool comparison per editor tick; cheap. Re-runs ApplyState only on
        // actual transitions so the inspector affordance pipeline does not churn on
        // every preview frame (preview already redraws fields via the change processor).
        var inMode = AnimationMode.InAnimationMode();
        if (inMode == m_LastInAnimationMode)
            return;
        m_LastInAnimationMode = inMode;
        ApplyState();
    }

    void OnTargetPropertyChanged(object sender, BindablePropertyChangedEventArgs e)
    {
        if (m_Inspector == null)
            return;
        if (e.propertyName == VisualElementSelection.ElementProperty)
            m_Inspector.Element = Target.Element;
        ApplyState();
    }

    /// <summary>
    /// Computes effective edit flags and animation recording state, then applies both to the inspector.
    /// When animation recording is active and the element is animatable, edit flags are overridden to
    /// <see cref="VisualElementEditFlags.Styles"/> regardless of the base flags on
    /// <see cref="VisualElementSelection"/>. The selection itself is never modified.
    /// </summary>
    void ApplyState()
    {
        if (m_Inspector == null)
            return;

        var controller = UIToolkitProjectSettings.s_EnablePanelRendererAnimationAtBoot
            ? StyleInspectorAnimationRecordingContext.TryCreateForElement(Target.Element)
            : null;

        bool inStagingMode = StageUtility.GetCurrentStage() is VisualElementEditingStage;
        bool animatable = controller != null && controller.HasRecordableProperties;
        bool overrideEditFlags = controller != null && (!inStagingMode || animatable);
        if (overrideEditFlags)
            m_Inspector.EditFlags = animatable ? VisualElementEditFlags.Styles : VisualElementEditFlags.None;
        else
            m_Inspector.EditFlags = Target.EditFlags;

        m_Inspector.RefreshRecordingState(controller);
    }

    protected override void OnHeaderGUI()
    {
        // Intentionally left empty to override the header.
    }

    // We don't want to have an artificial padding.
    public override bool UseDefaultMargins() => false;

    public override VisualElement CreateInspectorGUI()
    {
        m_Inspector = new VisualElementInspector { Element = Target.Element };
        ApplyState();
        Target.propertyChanged += OnTargetPropertyChanged;
        return m_Inspector;
    }

    void OnDestroy() => m_Inspector?.Dispose();

    public bool HasFrameBounds()
    {
        return GetBoundsInternal(out _);
    }

    public Bounds OnGetFrameBounds()
    {
        if (GetBoundsInternal(out var bounds))
            return bounds;

        return default;
    }

    bool GetBoundsInternal(out Bounds bounds)
    {
        bounds = default;

        var element = Target.Element;
        if (element == null)
            return false;

        var panelComponent = VisualElementSceneViewOverlay.FindPanelComponentForElement(element);
        if (panelComponent == null)
            return false;

        bounds = VisualElementSceneViewOverlay.GetElementWorldBounds(element, panelComponent);
        return true;

    }
}
