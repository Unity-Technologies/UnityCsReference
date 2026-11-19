// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// An inspector pane that shows what is selected in one <see cref="AuthoringContext"/>, with the same inspectors
/// the Inspector window uses for the selection proxies.
/// </summary>
/// <remarks>
/// <para>
/// Elements and style rules are shown by a <see cref="UISelectionInspectorContent"/> the pane keeps for each:
/// selecting another one only switches its target, rather than building an editor and its inspector again.
/// Any other object is shown by its editor, like in the Inspector window. Both find their context from their
/// target, so hosting them here needs nothing from them. The pane only follows
/// <see cref="IAuthoringSelection.Changed"/>.
/// </para>
/// <para>
/// None of the selection editors edits several objects at once, so a multi-selection shows no editor but says so,
/// like the Inspector window: objects of several types also get buttons that narrow the selection to one type.
/// </para>
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class AuthoringInspectorView : VisualElement, IDisposable
{
    public const string UssClassName = "unity-authoring-inspector-view";
    public const string EmptyLabelUssClassName = UssClassName + "__empty-label";
    public const string MultiSelectionUssClassName = UssClassName + "__multi-selection";
    public const string MultiSelectionHeaderUssClassName = UssClassName + "__multi-selection-header";
    public const string NarrowSelectionButtonUssClassName = UssClassName + "__narrow-selection-button";
    public const string ScrollViewUssClassName = UssClassName + "__scroll-view";

    internal const string rebuildMarkerName = "AuthoringInspectorView.Rebuild";
    [NoAutoStaticsCleanup] // immutable profiler marker, safe to persist
    static readonly ProfilerMarker k_RebuildMarker = new(rebuildMarkerName);

    // InspectorElement places a sticky header (the selection editors' header) in the container of the nearest
    // ancestor that has the main container class, as the Inspector window does.
    const string k_InspectorMainContainerUssClassName = "unity-inspector-main-container";
    const string k_InspectorStickyHeaderContainerUssClassName = "unity-inspector-sticky-header-container";

    // A proxy can be destroyed without the selection changing, when the element it stands for goes away.
    const long k_DestroyedTargetPollIntervalMs = 250;

    static readonly string k_NothingSelected = L10n.Tr("Select an element or a style rule to inspect it.", null);
    static readonly string k_MultiEditingNotSupported = L10n.Tr("Multi-object editing not supported.", null);
    static readonly string k_MixedTypesHeaderFormat = L10n.Tr("{0} Objects", null);
    static readonly string k_NarrowTheSelection = L10n.Tr("Narrow the Selection:", null);

    readonly AuthoringContext m_Context;
    readonly VisualElement m_StickyHeaderContainer;
    readonly VisualElement m_MultiSelection;
    readonly Label m_MultiSelectionHeader;
    readonly Label m_NarrowSelectionLabel;
    readonly VisualElement m_NarrowSelectionButtons;
    readonly ScrollView m_ScrollView;
    readonly Label m_EmptyLabel;
    readonly List<UnityEngine.Object> m_SelectedObjects = new();

    // Built on first use, and kept while the pane lives.
    readonly Dictionary<Type, ContentHost> m_Contents = new();

    ContentHost m_ActiveContent;
    UnityEditor.Editor m_Editor;
    InspectorElement m_InspectorElement;
    IVisualElementScheduledItem m_DestroyedTargetPoll;
    bool m_Subscribed;
    bool m_Disposed;

    public AuthoringInspectorView(AuthoringContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));

        AddToClassList(UssClassName);
        AddToClassList(k_InspectorMainContainerUssClassName);

        // Override bars line up on the left edge of this class's element, else of the panel, where this view may not be.
        AddToClassList(OverrideBarManipulator.overrideBarUssClassName);
        style.flexGrow = 1;

        // Only the scroll view gives up height: with its content as flex basis, it would shrink the header too.
        m_StickyHeaderContainer = new VisualElement();
        m_StickyHeaderContainer.AddToClassList(k_InspectorStickyHeaderContainerUssClassName);
        m_StickyHeaderContainer.style.flexShrink = 0;
        Add(m_StickyHeaderContainer);

        m_MultiSelection = new VisualElement();
        m_MultiSelection.AddToClassList(MultiSelectionUssClassName);
        m_MultiSelection.style.flexShrink = 0;
        m_MultiSelection.style.paddingLeft = 4;
        m_MultiSelection.style.paddingRight = 4;
        m_MultiSelection.style.paddingTop = 4;
        m_MultiSelection.style.display = DisplayStyle.None;
        Add(m_MultiSelection);

        m_MultiSelectionHeader = new Label();
        m_MultiSelectionHeader.AddToClassList(MultiSelectionHeaderUssClassName);
        m_MultiSelectionHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        m_MultiSelectionHeader.style.marginBottom = 4;
        m_MultiSelection.Add(m_MultiSelectionHeader);

        m_MultiSelection.Add(new HelpBox(k_MultiEditingNotSupported, HelpBoxMessageType.Info));

        m_NarrowSelectionLabel = new Label(k_NarrowTheSelection);
        m_NarrowSelectionLabel.style.marginTop = 6;
        m_MultiSelection.Add(m_NarrowSelectionLabel);

        m_NarrowSelectionButtons = new VisualElement();
        m_MultiSelection.Add(m_NarrowSelectionButtons);

        m_ScrollView = new ScrollView(ScrollViewMode.Vertical);
        m_ScrollView.AddToClassList(ScrollViewUssClassName);
        m_ScrollView.style.flexGrow = 1;
        m_ScrollView.style.flexShrink = 1;
        m_ScrollView.style.flexBasis = 0;
        Add(m_ScrollView);

        m_EmptyLabel = new Label(k_NothingSelected);
        m_EmptyLabel.AddToClassList(EmptyLabelUssClassName);
        m_EmptyLabel.style.flexGrow = 1;
        m_EmptyLabel.style.whiteSpace = WhiteSpace.Normal;
        m_EmptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        Add(m_EmptyLabel);

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    public AuthoringContext Context => m_Context;

    /// <summary>The object on display, or null when nothing or several objects are selected.</summary>
    public UnityEngine.Object Target => m_SelectedObjects.Count == 1 ? m_SelectedObjects[0] : null;

    /// <summary>
    /// The content that shows the element or style rule on display, or null when the pane shows something else.
    /// </summary>
    public UISelectionInspectorContent Content => m_SelectedObjects.Count == 1 ? m_ActiveContent?.Content : null;

    /// <summary>
    /// The editor of the object on display, or null when nothing or several objects are selected, or a
    /// <see cref="Content"/> shows the object.
    /// </summary>
    public UnityEditor.Editor Editor => m_Editor;

    public InspectorElement InspectorElement => m_InspectorElement;

    /// <summary>
    /// Moves the focus to the search field of the inspector on display.
    /// </summary>
    /// <returns>Whether the inspector on display has a search field.</returns>
    public bool FocusSearchField()
    {
        if (m_SelectedObjects.Count != 1 || this.Q<InspectorSearchField>() is not { } searchField)
            return false;

        searchField.toolbarSearchField.Focus();
        return true;
    }

    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        if (m_Disposed)
            return;

        Subscribe();
        Rebuild();
        m_DestroyedTargetPoll = schedule.Execute(RebuildIfTargetDestroyed).Every(k_DestroyedTargetPollIntervalMs);
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        Unsubscribe();
        m_DestroyedTargetPoll?.Pause();
        m_DestroyedTargetPoll = null;
        DestroyEditor();
        if (m_ActiveContent != null)
            m_ActiveContent.Content.Target = null;
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        Unsubscribe();
        DestroyEditor();
        HideContent();
        foreach (var host in m_Contents.Values)
        {
            host.Content.Dispose();
        }

        m_Contents.Clear();
    }

    void Subscribe()
    {
        if (m_Subscribed)
            return;

        m_Subscribed = true;
        m_Context.Selection.Changed += Rebuild;
    }

    void Unsubscribe()
    {
        if (!m_Subscribed)
            return;

        m_Subscribed = false;
        m_Context.Selection.Changed -= Rebuild;
    }

    /// <summary>
    /// Shows the selected object: an element or a style rule in the content kept for its type, anything else in
    /// its editor, keeping the current one when it already shows it. Shows the multi-selection message when several
    /// objects are selected.
    /// </summary>
    public void Rebuild()
    {
        if (m_Disposed || m_Context.IsDisposed)
            return;

        using var _ = k_RebuildMarker.Auto();
        m_SelectedObjects.Clear();
        foreach (var obj in m_Context.Selection.Objects)
        {
            if (obj != null)
                m_SelectedObjects.Add(obj);
        }

        if (m_SelectedObjects.Count != 1)
        {
            // The content stays without a target, ready for the next selection of its type.
            DestroyEditor();
            if (m_ActiveContent != null)
                m_ActiveContent.Content.Target = null;
        }
        else if (TryGetContent(m_SelectedObjects[0], out var host))
        {
            DestroyEditor();
            ShowContent(host);
            host.Content.Target = (UISelectionObject)m_SelectedObjects[0];
        }
        else if (m_Editor == null || m_Editor.target != m_SelectedObjects[0])
        {
            HideContent();
            DestroyEditor();
            m_Editor = UnityEditor.Editor.CreateEditor(m_SelectedObjects[0]);
            m_InspectorElement = new InspectorElement(m_Editor);
            m_ScrollView.Add(m_InspectorElement);
        }

        if (m_SelectedObjects.Count > 1)
            RefreshMultiSelection();

        m_EmptyLabel.style.display = m_SelectedObjects.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        m_StickyHeaderContainer.style.display = m_SelectedObjects.Count == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        m_ScrollView.style.display = m_SelectedObjects.Count == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        m_MultiSelection.style.display = m_SelectedObjects.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    bool TryGetContent(UnityEngine.Object obj, out ContentHost host)
    {
        var type = obj.GetType();
        if (m_Contents.TryGetValue(type, out host))
            return true;

        UISelectionInspectorContent content = obj switch
        {
            VisualElementSelection => new VisualElementSelectionInspectorContent(),
            StyleRuleSelection => new StyleRuleSelectionInspectorContent(m_Context),
            _ => null,
        };

        if (content == null)
            return false;

        host = new ContentHost(content);
        m_Contents.Add(type, host);
        return true;
    }

    void ShowContent(ContentHost host)
    {
        if (m_ActiveContent == host)
            return;

        HideContent();
        m_ActiveContent = host;
        m_StickyHeaderContainer.Add(host.Content.Header);
        m_ScrollView.Add(host.Body);
    }

    void HideContent()
    {
        if (m_ActiveContent == null)
            return;

        m_ActiveContent.Content.Target = null;
        m_ActiveContent.Content.Header.RemoveFromHierarchy();
        m_ActiveContent.Body.RemoveFromHierarchy();
        m_ActiveContent = null;
    }


    void RefreshMultiSelection()
    {
        var groups = GroupByType(m_SelectedObjects);
        m_MultiSelectionHeader.text = groups.Count == 1
            ? GetGroupLabel(groups[0])
            : string.Format(k_MixedTypesHeaderFormat, m_SelectedObjects.Count);

        // Like the Inspector window, only objects of several types offer to narrow the selection.
        m_NarrowSelectionButtons.Clear();
        var mixedTypes = groups.Count > 1;
        m_NarrowSelectionLabel.style.display = mixedTypes ? DisplayStyle.Flex : DisplayStyle.None;
        if (!mixedTypes)
            return;

        foreach (var group in groups)
        {
            var button = new Button(() => NarrowSelection(group)) { text = GetGroupLabel(group) };
            button.AddToClassList(NarrowSelectionButtonUssClassName);
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_NarrowSelectionButtons.Add(button);
        }
    }

    // Sorted like the Inspector window: the largest group first, then by label.
    static List<List<UnityEngine.Object>> GroupByType(List<UnityEngine.Object> objects)
    {
        var groups = new List<List<UnityEngine.Object>>();
        foreach (var obj in objects)
        {
            var group = groups.Find(g => g[0].GetType() == obj.GetType());
            if (group == null)
            {
                group = new List<UnityEngine.Object>();
                groups.Add(group);
            }

            group.Add(obj);
        }

        groups.Sort((x, y) => x.Count != y.Count
            ? y.Count.CompareTo(x.Count)
            : string.CompareOrdinal(GetGroupLabel(x), GetGroupLabel(y)));
        return groups;
    }

    static string GetGroupLabel(List<UnityEngine.Object> group)
    {
        var typeName = GetTypeDisplayName(group[0]);
        return $"{group.Count} {(group.Count > 1 ? typeName + "s" : typeName)}";
    }

    // A proxy is named after what it selects: a VisualElementSelection is a Visual Element.
    static string GetTypeDisplayName(UnityEngine.Object obj)
    {
        const string proxySuffix = "Selection";
        var typeName = obj.GetType().Name;
        if (obj is UISelectionObject && typeName.EndsWith(proxySuffix, StringComparison.Ordinal))
            typeName = typeName.Substring(0, typeName.Length - proxySuffix.Length);
        return ObjectNames.NicifyVariableName(typeName);
    }

    void NarrowSelection(List<UnityEngine.Object> objects)
    {
        var entityIds = new EntityId[objects.Count];
        for (var i = 0; i < objects.Count; ++i)
        {
            entityIds[i] = objects[i].GetEntityId();
        }

        m_Context.Selection.EntityIds = entityIds;
    }

    void RebuildIfTargetDestroyed()
    {
        // A destroyed object reads as null while this view still references it.
        foreach (var obj in m_SelectedObjects)
        {
            if (obj == null)
            {
                Rebuild();
                return;
            }
        }
    }

    void DestroyEditor()
    {
        if (m_InspectorElement != null)
        {
            m_InspectorElement.RemoveFromHierarchy();
            m_InspectorElement = null;

            // The sticky header lives outside the InspectorElement, which only removes it on detach.
            m_StickyHeaderContainer.Clear();
        }

        if (m_Editor != null)
        {
            UnityEngine.Object.DestroyImmediate(m_Editor);
            m_Editor = null;
        }
    }

    // The body wraps the inspector the way an InspectorElement wraps a custom inspector, for the same styles.
    sealed class ContentHost
    {
        public readonly UISelectionInspectorContent Content;
        public readonly VisualElement Body;

        public ContentHost(UISelectionInspectorContent content)
        {
            Content = content;
            Body = new VisualElement { pickingMode = PickingMode.Ignore };
            Body.AddToClassList(InspectorElement.ussClassName);
            Body.AddToClassList(InspectorElement.uIECustomVariantUssClassName);
            content.Inspector.AddToClassList(InspectorElement.customInspectorUssClassName);
            Body.Add(content.Inspector);
        }
    }
}
