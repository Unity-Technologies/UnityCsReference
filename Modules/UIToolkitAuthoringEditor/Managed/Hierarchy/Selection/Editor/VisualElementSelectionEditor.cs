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
    VisualElementHeader m_Header;
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
        {
            m_Header.Element = Target.Element;
            m_Inspector.Element = Target.Element;
        }
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

        // The controller will be null if the project setting is disabled or when we are not recording.
        // When the controller exists, we need to change the inspector visibility in two cases:
        // - When something can be animated and we are not in staging, we need to unlock the style section (everything disabled by default).
        // - When in staging and recording, we disable the sections other than the styles, locking sections that can't be recorded.
        bool inStagingMode = StageUtility.GetCurrentStage() is VisualElementEditingStage;
        bool animatable = controller != null && controller.HasRecordableProperties;

        bool overrideEditFlags = controller != null && (!inStagingMode || animatable);
        var editFlags = overrideEditFlags
            ? (animatable ? VisualElementEditFlags.Styles : VisualElementEditFlags.None)
            : Target.EditFlags;

        m_Inspector.EditFlags = editFlags;
        m_Inspector.RefreshRecordingState(controller);

        m_Header.SetEditState(editFlags);

        bool isRecording = controller != null;
        m_Header.UpdateAssetVisibility(editFlags, isRecording, inStagingMode);
    }

    // We don't want to have an artificial padding.
    public override bool UseDefaultMargins() => false;

    internal override bool isHeaderSticky => true;

    internal override VisualElement CreateInspectorHeaderGUI()
    {
        m_Header = new VisualElementHeader { name = "Header" };
        m_Header.Element = Target.Element;
        return m_Header;
    }

    public override VisualElement CreateInspectorGUI()
    {
        m_Inspector = new VisualElementInspector();
        m_Inspector.InitializeSearchField(m_Header.SearchField);
        m_Header.AttributesView.ShareContext(m_Inspector.AttributesInspector.AttributesView);
        m_Inspector.Element = Target.Element;
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
