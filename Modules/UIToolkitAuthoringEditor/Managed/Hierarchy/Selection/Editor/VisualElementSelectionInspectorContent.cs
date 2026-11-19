// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The header and the inspector of a <see cref="VisualElementSelection"/>.
/// </summary>
sealed class VisualElementSelectionInspectorContent : UISelectionInspectorContent
{
    readonly VisualElementHeader m_Header;
    readonly VisualElementInspector m_Inspector;
    VisualElement m_NameTrackedElement;
    bool m_StateRefreshScheduled;

    public override VisualElement Header => m_Header;

    public override UIInspector Inspector => m_Inspector;

    public new VisualElementSelection Target
    {
        get => (VisualElementSelection)base.Target;
        set => base.Target = value;
    }

    public VisualElementSelectionInspectorContent()
    {
        m_Header = new VisualElementHeader { name = "Header" };
        m_Inspector = new VisualElementInspector();
        m_Inspector.InitializeSearchField(m_Header.SearchField);
        m_Header.AttributesView.ShareContext(m_Inspector.AttributesInspector.AttributesView);
    }

    protected override StyleInspectorAnimationRecordingContext CreateRecordingContext()
        => StyleInspectorAnimationRecordingContext.TryCreateForElement(Target.Element);

    protected override void OnTargetChanged(UISelectionObject previous)
    {
        if (!ReferenceEquals(previous, null))
            previous.propertyChanged -= OnTargetPropertyChanged;

        var target = Target;
        if (!ReferenceEquals(target, null))
            target.propertyChanged += OnTargetPropertyChanged;

        ShowElement(target != null ? target.Element : null);
    }

    void ShowElement(VisualElement element)
    {
        m_Header.Element = element;
        m_Inspector.Element = element;
        TrackNameChanges(element);
    }

    void OnTargetPropertyChanged(object sender, BindablePropertyChangedEventArgs e)
    {
        if (e.propertyName == VisualElementSelection.ElementProperty)
            ShowElement(Target.Element);

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
        if (evt.property != VisualElement.nameProperty || m_StateRefreshScheduled)
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

        if (Target == null)
            return;

        var controller = CreateRecordingContext();

        // The controller will be null when we are not recording.
        // When the controller exists, we need to change the inspector visibility in two cases:
        // - When something can be animated and we are not in staging, we need to unlock the style section (everything disabled by default).
        // - When in staging and recording, we disable the sections other than the styles, locking sections that can't be recorded.
        bool inStagingMode = Context.IsEditingDocument;
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
}
