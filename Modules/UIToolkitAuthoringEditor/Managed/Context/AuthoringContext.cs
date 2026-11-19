// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The selection and the context-scoped messages that a set of authoring views share.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Global"/> is the in-scene workflow: the scene and the UI Stage, the Hierarchy and Inspector windows,
/// the UI Viewport and the StyleSheets window. It wraps the editor selection and the command system behind
/// <see cref="UICommandQueue"/>. A local context has its own selection and messages, and claims the elements it
/// owns so code acting on them finds it with <see cref="For(VisualElement)"/>.
/// </para>
/// <para>
/// Commands that modify assets always go through <see cref="UICommandQueue"/>, whatever context made them, so
/// every view of an asset refreshes. Only queries and messages about one set of views go through
/// <see cref="Messages"/>.
/// </para>
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed partial class AuthoringContext : IDisposable
{
    [NoAutoStaticsCleanup] // wraps the editor selection and the global command system, both of which persist
    static readonly AuthoringContext s_Global = new(new GlobalAuthoringSelection(), UICommandQueue.CommandSystem);

    [AutoStaticsCleanupOnCodeReload]
    [IgnoreForUAL0015("Registry of roots claimed by live authoring views; cleared on code reload and refilled as the views are rebuilt")]
    static readonly Dictionary<VisualElement, AuthoringContext> s_ClaimedRoots = new();

    readonly HashSet<VisualElement> m_ClaimedRoots = new();
    UIDocumentSession m_Session;

    public static AuthoringContext Global => s_Global;

    public IAuthoringSelection Selection { get; }

    public CommandSystem Messages { get; }

    public bool IsGlobal => ReferenceEquals(this, s_Global);

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Whether this context edits a document directly, as the UI Stage does, rather than the scene.
    /// </summary>
    public bool IsEditingDocument => Session != null;

    /// <summary>
    /// The session this context edits its document through, or <see langword="null"/> when it edits none.
    /// </summary>
    /// <remarks>
    /// For <see cref="Global"/>, the session of the current UI Stage. For a local context, the session created with
    /// it as owner.
    /// </remarks>
    public UIDocumentSession Session => IsGlobal
        ? (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.GetOrCreateSession()
        : m_Session;

    /// <summary>
    /// The handler of the hierarchy listing this context's elements, which applies the context's pending selection
    /// requests. Set by the handler itself. <see cref="Global"/> uses the Hierarchy window's instead.
    /// </summary>
    internal VisualElementNodeHandler ElementHandler { get; set; }

    /// <summary>
    /// The document this context edits, or <see langword="null"/> when it edits none.
    /// </summary>
    public VisualTreeAsset EditedVisualTreeAsset => Session?.EditedVisualTreeAsset;

    /// <summary>
    /// The panel settings the edited document is previewed with, or <see langword="null"/>.
    /// </summary>
    public PanelSettings PanelSettings => TryGetDocument(out var document) ? document.PanelSettings : null;

    /// <summary>
    /// Creates a local context, with a selection and messages of its own.
    /// </summary>
    public AuthoringContext()
        : this(new LocalAuthoringSelection())
    {
    }

    /// <summary>
    /// Creates a local context on <paramref name="selection"/>, with messages of its own.
    /// </summary>
    public AuthoringContext(LocalAuthoringSelection selection)
        : this(selection ?? throw new ArgumentNullException(nameof(selection)), new CommandSystem())
    {
        HighlightResolver.Register(this);
        if (VisualElementSelectionRegistry.Instance != null)
            VisualElementSelectionRegistry.Instance.SelectionObjectsDestroyed += selection.RemoveDestroyedObjects;
    }

    AuthoringContext(IAuthoringSelection selection, CommandSystem messages)
    {
        Selection = selection;
        Messages = messages;
    }

    /// <summary>
    /// The context that owns <paramref name="element"/>, or <see cref="Global"/> when no context claims it.
    /// </summary>
    /// <remarks>
    /// Walks up the hierarchy to a claimed element. The root of a <see cref="PanelElement"/>'s sub-panel continues
    /// from the <see cref="PanelElement"/> itself, so claiming the element that hosts the canvas covers the
    /// document rendered in it, including after the sub-panel is recreated.
    /// </remarks>
    public static AuthoringContext For(VisualElement element)
    {
        if (s_ClaimedRoots.Count == 0)
            return s_Global;

        var current = element;
        while (current != null)
        {
            if (s_ClaimedRoots.TryGetValue(current, out var context))
                return context;

            current = current.hierarchy.parent
                ?? (current.panel as PanelElement.IPanelElementSubPanel)?.Owner;
        }

        return s_Global;
    }

    public static AuthoringContext For(IPanel panel) => For(panel?.visualTree);

    /// <summary>
    /// The context a selection object belongs to: the one owning its element, else the one that created it.
    /// </summary>
    public static AuthoringContext For(UISelectionObject selectionObject)
    {
        if (selectionObject is VisualElementSelection { Element: { } element })
            return For(element);

        return selectionObject != null && selectionObject.OwnerContext is { IsDisposed: false } owner
            ? owner
            : s_Global;
    }

    /// <summary>
    /// Makes this context the owner of <paramref name="root"/> and everything under it.
    /// </summary>
    public void Claim(VisualElement root)
    {
        if (IsGlobal)
            throw new InvalidOperationException("The global context owns everything no other context claims.");
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AuthoringContext));
        if (root == null)
            throw new ArgumentNullException(nameof(root));
        if (s_ClaimedRoots.TryGetValue(root, out var owner) && owner != this)
            throw new InvalidOperationException("The element is already claimed by another context.");

        s_ClaimedRoots[root] = this;
        m_ClaimedRoots.Add(root);
    }

    public void Release(VisualElement root)
    {
        if (root == null || !m_ClaimedRoots.Remove(root))
            return;

        s_ClaimedRoots.Remove(root);
    }

    /// <summary>
    /// The editing context of the document <see cref="Session"/> edits.
    /// </summary>
    public bool TryGetDocument(out VisualTreeAssetEditingContext document)
    {
        var session = Session;
        document = session?.Document ?? default;
        return session != null;
    }

    // Called by a session created with this context as owner.
    internal void SetSession(UIDocumentSession session)
    {
        if (IsGlobal)
            throw new InvalidOperationException("The global context follows the current UI Stage.");
        if (m_Session != null && m_Session != session)
            throw new InvalidOperationException("The context already has a session.");

        m_Session = session;
    }

    internal void ClearSession(UIDocumentSession session)
    {
        if (m_Session == session)
            m_Session = null;
    }

    /// <summary>
    /// What can be edited on <paramref name="element"/> in this context.
    /// </summary>
    public VisualElementEditFlags GetEditFlags(VisualElement element)
    {
        if (Session is { } session)
            return session.GetElementEditFlags(element);

        return IsGlobal ? UIToolkitStageUtility.GetMainStageEditFlags(element) : VisualElementEditFlags.None;
    }

    /// <summary>
    /// Disposes the session, releases the claims, and clears the selection. Does nothing for <see cref="Global"/>.
    /// </summary>
    public void Dispose()
    {
        if (IsGlobal || IsDisposed)
            return;

        if (Selection is LocalAuthoringSelection selection && VisualElementSelectionRegistry.Instance != null)
            VisualElementSelectionRegistry.Instance.SelectionObjectsDestroyed -= selection.RemoveDestroyedObjects;

        m_Session?.Dispose();
        m_Session = null;

        IsDisposed = true;
        HighlightResolver.Unregister(this);

        foreach (var root in m_ClaimedRoots)
            s_ClaimedRoots.Remove(root);
        m_ClaimedRoots.Clear();

        // The undo history keeps the last selection, for the next context its owner builds.
        if (Selection is LocalAuthoringSelection localSelection)
            localSelection.UndoState = null;
        Selection.Clear();
    }
}
