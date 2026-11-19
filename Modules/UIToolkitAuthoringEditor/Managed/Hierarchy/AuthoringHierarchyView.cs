// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The hierarchy pane of a window that edits one <see cref="UIDocumentSession"/> on a local context: the rows of
/// the session's document, with the editing, drag and drop and context menus of the Hierarchy window, and a
/// selection that stays in the session's context.
/// </summary>
/// <remarks>
/// The view owns its <see cref="Unity.Hierarchy.Hierarchy"/>, which is what keeps it apart from the Hierarchy
/// window and its stages. Its host pumps it (<see cref="Update"/>), the way an editor window pumps the
/// hierarchy it shows.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class AuthoringHierarchyView : VisualElement, IDisposable
{
    public const string UssClassName = "unity-authoring-hierarchy-view";
    public const string SearchFieldUssClassName = UssClassName + "__search-field";
    public const string HierarchyViewUssClassName = UssClassName + "__hierarchy-view";

    internal const string updateMarkerName = "AuthoringHierarchyView.Update";
    [NoAutoStaticsCleanup] // immutable profiler marker, safe to persist
    static readonly ProfilerMarker k_UpdateMarker = new(updateMarkerName);

    readonly UIDocumentSession m_Session;
    readonly Unity.Hierarchy.Hierarchy m_Hierarchy;
    readonly UIDocumentSessionNodeHandler m_Handler;
    readonly HierarchyView m_HierarchyView;
    readonly HierarchyLocalSelectionHandler m_SelectionHandler;
    readonly ToolbarSearchField m_SearchField;

    bool m_Disposed;

    /// <param name="session">The session whose document the view shows.</param>
    /// <param name="showDocumentNode">
    /// Whether the document gets a row of its own, holding its root elements. Otherwise they are the top rows.
    /// </param>
    public AuthoringHierarchyView(UIDocumentSession session, bool showDocumentNode = false)
    {
        m_Session = session ?? throw new ArgumentNullException(nameof(session));

        AddToClassList(UssClassName);
        style.flexGrow = 1;

        m_SearchField = new ToolbarSearchField();
        m_SearchField.AddToClassList(SearchFieldUssClassName);
        m_SearchField.style.width = StyleKeyword.Auto;
        m_SearchField.RegisterValueChangedCallback(evt => m_HierarchyView.Filter = evt.newValue ?? string.Empty);
        Add(m_SearchField);

        m_Hierarchy = new Unity.Hierarchy.Hierarchy();
        m_Handler = UIDocumentSessionNodeHandler.Create(m_Hierarchy, session, showDocumentNode);
        m_Hierarchy.Update();

        m_HierarchyView = new HierarchyView();
        m_HierarchyView.AddToClassList(HierarchyViewUssClassName);
        m_HierarchyView.style.flexGrow = 1;
        m_HierarchyView.SetSourceHierarchy(m_Hierarchy);

        // The navigate column beside Name opens template instances in the UI Stage; the pane edits its own
        // session and binds none.
        m_HierarchyView.SetColumns(new List<Column> { m_HierarchyView.NameColumn });
        m_HierarchyView.NameColumn.stretchable = true;
        m_HierarchyView.ListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
        Add(m_HierarchyView);

        m_SelectionHandler = new HierarchyLocalSelectionHandler(m_HierarchyView, m_Hierarchy, Context.Selection)
        {
            StandIn = GetDocumentStandIn,
        };
        m_HierarchyView.EnqueuePostUpdateAction(() => m_SelectionHandler.SyncViewModelFromSelection(frameSelection: false));

        RegisterCallback<PointerUpEvent>(OnPointerUp);
        RegisterCallback<KeyUpEvent>(OnKeyUp);
        RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
        RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        m_HierarchyView.ListView.RegisterCallback<PointerDownEvent>(OnListViewPointerDown);
        m_HierarchyView.ListView.RegisterCallback<DragPerformEvent>(OnListViewDragPerformed, TrickleDown.TrickleDown);
    }

    public UIDocumentSession Session => m_Session;
    public AuthoringContext Context => m_Session.Owner;
    public Unity.Hierarchy.Hierarchy SourceHierarchy => m_Hierarchy;
    public HierarchyView HierarchyView => m_HierarchyView;
    public UIDocumentSessionNodeHandler Handler => m_Handler;
    public HierarchyLocalSelectionHandler SelectionHandler => m_SelectionHandler;
    public ToolbarSearchField SearchField => m_SearchField;

    /// <summary>
    /// The object the selection holds while the document row is selected, which the row is selected for in turn.
    /// Without one, or without a document row, selecting the row selects nothing.
    /// </summary>
    public Func<UnityEngine.Object> DocumentSelectionObject { get; set; }

    (HierarchyNode, EntityId) GetDocumentStandIn()
    {
        var selectionObject = DocumentSelectionObject?.Invoke();
        if (selectionObject == null || m_Disposed || !m_Handler.TryGetDocumentNode(out var node))
            return (HierarchyNode.Null, EntityId.None);

        return (node, selectionObject.GetEntityId());
    }

    /// <summary>
    /// Brings the rows up to date with the document, and applies the selection changes made since.
    /// </summary>
    public void Update()
    {
        if (m_Disposed)
            return;

        if (m_HierarchyView.UpdateNeeded)
        {
            using var _ = k_UpdateMarker.Auto();
            m_HierarchyView.Update();
        }
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        m_HierarchyView.ListView.UnregisterCallback<PointerDownEvent>(OnListViewPointerDown);
        m_HierarchyView.ListView.UnregisterCallback<DragPerformEvent>(OnListViewDragPerformed, TrickleDown.TrickleDown);
        m_SelectionHandler.Dispose();
        m_HierarchyView.Dispose();
        m_Hierarchy.Dispose();
    }

    void OnPointerUp(PointerUpEvent evt) => m_SelectionHandler.SyncSelectionFromViewModel();

    void OnKeyUp(KeyUpEvent evt)
    {
        switch (evt.keyCode)
        {
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
            case KeyCode.Home:
            case KeyCode.End:
            case KeyCode.PageUp:
            case KeyCode.PageDown:
            case KeyCode.Escape:
                m_SelectionHandler.SyncSelectionFromViewModel();
                break;
        }
    }

    // A right-click selects the row under it before the menu opens, and the menu acts on the context selection.
    void OnListViewPointerDown(PointerDownEvent evt)
    {
        if (HierarchyViewUtility.IsContextClick((MouseButton)evt.button, evt.modifiers))
            m_SelectionHandler.SyncSelectionFromViewModel();
    }

    void OnListViewDragPerformed(DragPerformEvent evt)
    {
        if (m_HierarchyView.ViewModel.HasFlags(HierarchyNodeFlags.Selected))
            m_SelectionHandler.SyncSelectionFromViewModel();
    }

    /// <summary>
    /// Whether the view runs <paramref name="commandName"/>, one of the <see cref="EventCommandNames"/>.
    /// </summary>
    public static bool AcceptsCommand(string commandName)
    {
        switch (commandName)
        {
            case EventCommandNames.Find:
            case EventCommandNames.FrameSelected:
            case EventCommandNames.Cut:
            case EventCommandNames.Copy:
            case EventCommandNames.Paste:
            case EventCommandNames.Rename:
            case EventCommandNames.Duplicate:
            case EventCommandNames.Delete:
            case EventCommandNames.SoftDelete:
            case EventCommandNames.SelectAll:
            case EventCommandNames.DeselectAll:
            case EventCommandNames.InvertSelection:
            case EventCommandNames.SelectChildren:
                return true;

            default:
                return false;
        }
    }

    void OnValidateCommand(ValidateCommandEvent evt)
    {
        if (AcceptsCommand(evt.commandName))
            evt.StopPropagation();
    }

    void OnExecuteCommand(ExecuteCommandEvent evt)
    {
        if (ExecuteCommand(evt.commandName))
            evt.StopPropagation();
    }

    /// <summary>
    /// Runs <paramref name="commandName"/> on the rows selected in the view, as if it was sent to the view. For a
    /// host that takes the commands no other pane took.
    /// </summary>
    /// <returns>Whether the view runs the command.</returns>
    public bool ExecuteCommand(string commandName)
    {
        if (m_Disposed)
            return false;

        switch (commandName)
        {
            case EventCommandNames.Find:
                m_SearchField.Focus();
                break;

            case EventCommandNames.FrameSelected:
                FrameSelectedNodes();
                break;

            case EventCommandNames.Cut:
                m_HierarchyView.OnCut();
                break;

            case EventCommandNames.Copy:
                m_HierarchyView.OnCopy();
                break;

            case EventCommandNames.Paste:
                m_HierarchyView.OnPaste();
                break;

            case EventCommandNames.Rename:
                if (m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected) == 1)
                {
                    Span<HierarchyNode> nodes = stackalloc HierarchyNode[1];
                    m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
                    m_HierarchyView.OnSetName(nodes[0]);
                }
                break;

            case EventCommandNames.Duplicate:
                m_HierarchyView.OnDuplicate();
                break;

            case EventCommandNames.Delete:
            case EventCommandNames.SoftDelete:
                m_HierarchyView.OnDelete();
                break;

            case EventCommandNames.SelectAll:
                m_HierarchyView.SelectAll(exposedOnly: true);
                m_SelectionHandler.SyncSelectionFromViewModel();
                break;

            case EventCommandNames.DeselectAll:
                m_HierarchyView.DeselectAll();
                m_SelectionHandler.SyncSelectionFromViewModel();
                break;

            case EventCommandNames.InvertSelection:
                m_HierarchyView.ToggleSelection();
                m_SelectionHandler.SyncSelectionFromViewModel();
                break;

            case EventCommandNames.SelectChildren:
                m_HierarchyView.SelectChildrenAndExpandRecursive();
                m_SelectionHandler.SyncSelectionFromViewModel();
                break;

            default:
                return false;
        }

        return true;
    }

    void FrameSelectedNodes()
    {
        var count = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
        if (count == 0)
            return;

        var nodes = new HierarchyNode[count];
        m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
        m_HierarchyView.Frame(nodes);
    }
}
