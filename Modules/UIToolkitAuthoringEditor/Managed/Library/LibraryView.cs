// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The library pane of a window that edits a document on a local <see cref="AuthoringContext"/>: the content of
/// the UI Library window, embedded, to drag into the canvas or the hierarchy or to add with a double-click.
/// </summary>
/// <remarks>
/// The view only adds to the document of the context that claims it, which <see cref="UIElementsProvider"/>
/// resolves from the view itself. Its items drag the same as the UI Library window's, into this window or any
/// other. It lists the editor controls when the document is an editor extension
/// (<see cref="EditorExtensionAuthoring"/>), and follows the document and the project on its own while it is
/// attached.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class LibraryView : VisualElement, IDisposable
{
    public const string UssClassName = "unity-library-view";
    public const string ResultsUssClassName = UssClassName + "__results";

    // Search slices its work over updates; the UI Library window runs one per editor update.
    const long k_SearchUpdateIntervalMs = 16;
    static readonly TimeSpan k_SearchUpdateTimeSlice = TimeSpan.FromMilliseconds(10);
    const long k_StatePollIntervalMs = 250;

    readonly AuthoringContext m_Context;
    readonly SearchView m_SearchView;
    readonly SearchToolbar m_Toolbar;
    readonly SearchGroupBar m_GroupBar;
    readonly IVisualElementScheduledItem m_SearchUpdate;
    readonly IVisualElementScheduledItem m_StatePoll;

    VisualTreeAsset m_Document;
    bool m_EditorExtensionMode;
    bool m_Disposed;

    public LibraryView(AuthoringContext context)
    {
        m_Context = context ?? throw new ArgumentNullException(nameof(context));

        AddToClassList(UssClassName);
        style.flexGrow = 1;
        SearchElement.AppendStyleSheets(this);

        m_SearchView = new SearchView(UIElementsProvider.CreateEmbeddedLibraryViewState(), EntityId.None);

        m_Toolbar = new SearchToolbar("SearchToolbar", m_SearchView);
        m_Toolbar.style.flexShrink = 0;
        Add(m_Toolbar);

        var results = new VisualElement { style = { flexGrow = 1 } };
        results.AddToClassList(ResultsUssClassName);
        results.AddToClassList("search-panel");
        results.AddToClassList("search-result-container");
        Add(results);

        m_GroupBar = new SearchGroupBar("SearchGroupbar", m_SearchView);
        m_GroupBar.SortingChanged += m_SearchView.SetSearchItemComparer;
        results.Add(m_GroupBar);
        results.Add(m_SearchView);
        UIElementsProvider.InstallControlsVisibilityDropdown(this, m_SearchView);

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

        m_SearchUpdate = schedule.Execute(() => m_SearchView.UpdateIncrementalTimed(k_SearchUpdateTimeSlice))
            .Every(k_SearchUpdateIntervalMs);
        m_StatePoll = schedule.Execute(UpdateDocumentState).Every(k_StatePollIntervalMs);

        UpdateDocumentState();
    }

    public AuthoringContext Context => m_Context;

    /// <summary>
    /// The search view listing the library, as the UI Library window does.
    /// </summary>
    public ISearchView SearchView => m_SearchView;

    /// <summary>
    /// The document the view adds to: the one its context's session edits.
    /// </summary>
    public VisualTreeAsset Document => m_Document;

    /// <summary>
    /// Whether the view lists the editor controls, which it does when <see cref="Document"/> is an editor
    /// extension.
    /// </summary>
    public bool EditorExtensionMode => m_EditorExtensionMode;

    /// <summary>
    /// Fetches the library again from the project and the document as they are now.
    /// </summary>
    public void Refresh()
    {
        if (m_Disposed)
            return;

        UpdateDocumentState();
        m_SearchView.Refresh(RefreshFlags.StructureChanged);
    }

    /// <summary>
    /// Adds <paramref name="item"/> to <see cref="Document"/>, the way a double-click does: see
    /// <see cref="MenuUtility.AddFromLibrary"/>.
    /// </summary>
    public void AddToDocument(LibraryItem item)
    {
        if (m_Disposed || !CanAdd(item))
            return;

        MenuUtility.AddFromLibrary(m_Context, item);
    }

    /// <summary>
    /// Whether <paramref name="item"/> can be added to <see cref="Document"/>. A document cannot instantiate
    /// itself, and a document that is only previewed takes nothing.
    /// </summary>
    public bool CanAdd(LibraryItem item) => UIElementsProvider.CanAddToLocalDocument(m_Context, item);

    /// <summary>
    /// The item of the row at <paramref name="worldPosition"/>; a folder row has none.
    /// </summary>
    public bool TryGetItemAtPosition(Vector2 worldPosition, out LibraryItem item)
    {
        item = null;
        if (m_Disposed || !m_SearchView.TryGetItemAtPosition(worldPosition, out var searchItem))
            return false;

        item = searchItem.data as LibraryItem;
        return item != null;
    }

    /// <summary>
    /// Runs the pending search to the end, instead of over the next updates.
    /// </summary>
    internal void CompleteSearch() => m_SearchView.Update();

    /// <summary>
    /// Moves the focus to the search field.
    /// </summary>
    public void FocusSearchField()
    {
        // SearchFieldElement.FocusSearchField only takes the focus in a search window. The query builder has a text
        // field of its own, which the plain one gives way to.
        m_Toolbar.Query<TextField>().Where(field => field.canGrabFocus).First()?.Focus();
    }

    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        m_SearchUpdate.Pause();
        m_StatePoll.Pause();
        m_GroupBar.SortingChanged -= m_SearchView.SetSearchItemComparer;
        m_SearchView.Dispose();
    }

    // The search runs once the view is attached, and again when the project changed while it was detached.
    void OnAttachToPanel(AttachToPanelEvent evt)
    {
        if (!m_Disposed)
            m_SearchView.Refresh();
    }

    // Follows the session's document, and its editor extension setting, which undo or another view can change.
    void UpdateDocumentState()
    {
        if (m_Disposed)
            return;

        var session = m_Context.IsDisposed ? null : m_Context.Session;
        var document = session is { IsDisposed: false } ? session.EditedVisualTreeAsset : null;
        var editorExtensionMode = EditorExtensionAuthoring.IsEnabled(document);
        if (document == m_Document && editorExtensionMode == m_EditorExtensionMode)
            return;

        var modeChanged = editorExtensionMode != m_EditorExtensionMode;
        m_Document = document;
        m_EditorExtensionMode = editorExtensionMode;
        if (modeChanged && panel != null)
            m_SearchView.Refresh(RefreshFlags.StructureChanged);
    }
}
