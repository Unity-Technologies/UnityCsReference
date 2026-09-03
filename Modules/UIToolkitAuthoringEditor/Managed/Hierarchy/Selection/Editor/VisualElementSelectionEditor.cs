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
class VisualElementSelectionEditor : UISelectionEditor
{
    VisualElementInspector m_Inspector;
    VisualElementHeader m_Header;
    VisualElement m_NameTrackedElement;
    bool m_StateRefreshScheduled;

    private VisualElementSelection Target => (VisualElementSelection)target;

    protected override UIInspector Inspector => m_Inspector;

    protected override StyleInspectorAnimationRecordingContext CreateRecordingContext()
        => StyleInspectorAnimationRecordingContext.TryCreateForElement(Target.Element);

    protected override void OnDisable()
    {
        base.OnDisable();
        Target.propertyChanged -= OnTargetPropertyChanged;
        TrackNameChanges(null);
    }

    void OnTargetPropertyChanged(object sender, BindablePropertyChangedEventArgs e)
    {
        if (m_Inspector == null)
            return;
        if (e.propertyName == VisualElementSelection.ElementProperty)
        {
            m_Header.Element = Target.Element;
            m_Inspector.Element = Target.Element;
            TrackNameChanges(Target.Element);
        }
        ApplyState();
    }

    void TrackNameChanges(VisualElement element)
    {
        if (m_NameTrackedElement == element)
            return;
        m_NameTrackedElement?.UnregisterCallback<PropertyChangedEvent>(OnElementPropertyChanged);
        m_NameTrackedElement = element;
        m_NameTrackedElement?.RegisterCallback<PropertyChangedEvent>(OnElementPropertyChanged);
    }

    // Naming an element makes the animation binder able to address it, which flips the recording banner
    // and the edit flags. Deferred a tick: the name lands mid-deserialization of the attribute field the
    // user just edited, and ApplyState reconfigures that same attributes view through the edit flags.
    void OnElementPropertyChanged(PropertyChangedEvent evt)
    {
        if (evt.property != VisualElement.nameProperty || m_StateRefreshScheduled || m_Inspector == null)
            return;

        m_StateRefreshScheduled = true;
        m_Inspector.schedule.Execute(ApplyState);
    }

    /// <summary>
    /// Computes effective edit flags and animation recording state, then applies both to the inspector.
    /// When animation recording is active and the element is animatable, edit flags are overridden to
    /// <see cref="VisualElementEditFlags.Styles"/> regardless of the base flags on
    /// <see cref="VisualElementSelection"/>. The selection itself is never modified.
    /// </summary>
    protected override void ApplyState()
    {
        m_StateRefreshScheduled = false;

        if (m_Inspector == null)
            return;

        var controller = CreateRecordingContextIfEnabled();

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
        TrackNameChanges(Target.Element);
        ApplyState();
        Target.propertyChanged += OnTargetPropertyChanged;
        return m_Inspector;
    }

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
        // Resolves the world-space scene instance (incl. from a staging clone), falls back to the host
        // GameObject, and floors the flat-quad bounds — so Edit > Frame Selected / F frame the element
        // properly in the SceneView instead of diving in or doing nothing.
        return VisualElementSceneViewOverlay.TryGetFramableWorldBounds(Target.Element, out bounds, out _);
    }
}
