// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Hierarchy;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The visual element rows of a hierarchy that shows one <see cref="UIDocumentSession"/>, outside the Hierarchy
/// window and its stages.
/// </summary>
/// <remarks>
/// A hierarchy creates its handlers itself, so the session is handed over through <see cref="Create"/>.
/// </remarks>
internal sealed class UIDocumentSessionNodeHandler : VisualElementNodeHandler
{
    internal new const string NodeTypeName = "UIDocumentSessionNodeHandler";

    [NoAutoStaticsCleanup] // only set while Create runs
    static UIDocumentSession s_PendingSession;
    [NoAutoStaticsCleanup] // only set while Create runs
    static bool s_PendingShowDocumentNode;

    UIDocumentSession m_Session;

    public UIDocumentSession Session => m_Session;

    public override string GetNodeTypeName() => NodeTypeName;

    /// <summary>
    /// Creates the handler of <paramref name="hierarchy"/> for <paramref name="session"/>.
    /// </summary>
    /// <param name="hierarchy">The hierarchy to create the handler of.</param>
    /// <param name="session">The session whose document the hierarchy shows.</param>
    /// <param name="showDocumentNode">Whether the document gets a node of its own, parent of its root elements.</param>
    public static UIDocumentSessionNodeHandler Create(Unity.Hierarchy.Hierarchy hierarchy, UIDocumentSession session,
        bool showDocumentNode = false)
    {
        if (hierarchy == null)
            throw new ArgumentNullException(nameof(hierarchy));
        if (session == null)
            throw new ArgumentNullException(nameof(session));
        if (hierarchy.GetNodeTypeHandlerBase<UIDocumentSessionNodeHandler>() != null)
            throw new InvalidOperationException("The hierarchy already shows a session.");

        s_PendingSession = session;
        s_PendingShowDocumentNode = showDocumentNode;
        try
        {
            return hierarchy.GetOrCreateNodeTypeHandler<UIDocumentSessionNodeHandler>();
        }
        finally
        {
            s_PendingSession = null;
            s_PendingShowDocumentNode = false;
        }
    }

    protected override NodeHandlerStageStrategy CreateStageStrategy()
    {
        m_Session = s_PendingSession
            ?? throw new InvalidOperationException($"Create the handler with {nameof(UIDocumentSessionNodeHandler)}.{nameof(Create)}.");
        return new UIDocumentSessionStrategy(m_Session, showsDocumentNode: s_PendingShowDocumentNode);
    }

    /// <summary>
    /// The node the document shows as, when the handler gives it one (see <see cref="Create"/>).
    /// </summary>
    public bool TryGetDocumentNode(out HierarchyNode node)
    {
        node = HierarchyNode.Null;
        if (m_Session is not { IsDisposed: false, Panel: PanelElement.IPanelElementSubPanel subPanel })
            return false;

        var root = subPanel.Root;
        return root != null && StageStrategy.GetDocumentNodeAsset(root) != null && TryGetNodeFromElement(root, out node);
    }
}
