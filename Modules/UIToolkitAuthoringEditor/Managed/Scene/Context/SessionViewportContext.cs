// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// A <see cref="UIDocumentSession"/> as seen by the viewport: the session owns the panel, the document and the
/// editing context, so this only forwards to it.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class SessionViewportContext : IUIViewportContext
{
    readonly UIDocumentSession m_Session;
    readonly object m_Source;
    readonly UnityEngine.Object m_SourceObject;
    readonly Action<UIViewport> m_PopulateBreadcrumbs;

    /// <param name="session">The session to show.</param>
    /// <param name="source">What owns the session, such as the stage. It decides whether two contexts are the same.</param>
    /// <param name="populateBreadcrumbs">
    /// Fills the breadcrumbs; without one, they only name the edited document.
    /// </param>
    public SessionViewportContext(UIDocumentSession session, object source, Action<UIViewport> populateBreadcrumbs = null)
    {
        m_Session = session ?? throw new ArgumentNullException(nameof(session));
        m_Source = source ?? session;
        m_SourceObject = source as UnityEngine.Object;
        m_PopulateBreadcrumbs = populateBreadcrumbs;
    }

    /// <summary>
    /// The context of the UI Stage <paramref name="stage"/>, whose breadcrumbs are the stage history.
    /// </summary>
    public static SessionViewportContext ForStage(VisualElementEditingStage stage)
        => new(stage.GetOrCreateSession(), stage, VisualElementEditingStage.PopulateHistoryBreadcrumbs);

    public UIDocumentSession Session => m_Session;

    public object Source => m_Source;

    public PanelElement PanelElement => m_Session.PanelElement;

    // The session's own panel is what the Hierarchy and the inspector already select into, so there is nothing
    // for the viewport to resolve through.
    public VisualElement AuthoritativeRoot => null;

    public VisualTreeAsset RootVisualTreeAsset => m_Session.IsDisposed ? null : m_Session.Document.RootVisualTreeAsset;

    public VisualTreeAsset EditedVisualTreeAsset => m_Session.IsDisposed ? null : m_Session.EditedVisualTreeAsset;

    public PanelSettings PanelSettings => m_Session.IsDisposed ? null : m_Session.Document.PanelSettings;

    public string HeaderTitle
    {
        get
        {
            if (!m_Session.IsDisposed && m_Session.StandaloneStyleSheet != null)
                return Path.GetFileName(AssetDatabase.GetAssetPath(m_Session.StandaloneStyleSheet));

            var document = EditedVisualTreeAsset;
            return document ? document.name + ".uxml" : string.Empty;
        }
    }

    public string CanvasStorageKey => m_Session.IsDisposed ? null : $"CanvasSettings-{m_Session.GetStateStorageHash()}";

    // A session owned by an object (the stage) dies with it.
    public bool IsValid => !m_Session.IsDisposed
        && m_Session.EditedVisualTreeAsset != null
        && (m_SourceObject is null || m_SourceObject != null);

    // Opening a session is itself the way to author a document, unless it only previews one.
    public bool AllowsAuthoring => !m_Session.IsDisposed && !m_Session.IsDocumentReadOnly;

    // The same scope as File > Save: the document together with its style sheets.
    public bool HasUnsavedChanges => !m_Session.IsDisposed && m_Session.HasUnsavedChanges;

    public bool Save() => !m_Session.IsDisposed && m_Session.Save();

    // The session's owner opens the panel and closes it; the viewport only borrows it.
    public void Acquire()
    {
    }

    public void Release()
    {
    }

    public void RequestRefresh() => m_Session.RequestRefresh();

    // Nothing resolved means we cannot prove the drop is safe; refuse it.
    public bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset)
        => m_Session.IsDisposed || m_Session.WillCauseCircularDependency(visualTreeAsset);

    public void PopulateBreadcrumbs(UIViewport viewport)
    {
        if (m_PopulateBreadcrumbs != null)
        {
            m_PopulateBreadcrumbs(viewport);
            return;
        }

        viewport.ClearBreadcrumbs();

        var document = EditedVisualTreeAsset;
        if (document)
        {
            var label = HasUnsavedChanges ? document.name + "*" : document.name;
            viewport.PushBreadcrumb(label, EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D);
        }
    }
}
