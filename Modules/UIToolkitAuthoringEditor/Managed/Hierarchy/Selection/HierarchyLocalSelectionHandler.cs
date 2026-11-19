// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Keeps the selection of a <see cref="HierarchyView"/> and an <see cref="IAuthoringSelection"/> in step, the way
/// <c>HierarchyGlobalSelectionHandler</c> does for the editor selection, for views that run on a local context.
/// </summary>
/// <remarks>
/// <para>
/// Like the global handler, it reads the view back only when asked (<see cref="SyncSelectionFromViewModel"/>): its
/// host calls it after the user changed the view selection. Listening to every flag change would also pick up the
/// flags the view drops when it rebuilds rows, and clear the selection the other panes show.
/// </para>
/// <para>
/// A selection change is applied to the view after its next update, when the nodes for newly created elements
/// exist. Several panes share one selection, so objects this view has no node for are left out of the view and
/// kept in the selection.
/// </para>
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class HierarchyLocalSelectionHandler : IDisposable
{
    readonly HierarchyView m_View;
    readonly Unity.Hierarchy.Hierarchy m_Hierarchy;
    readonly IAuthoringSelection m_Selection;

    bool m_WritingSelection;
    bool m_SyncPending;
    bool m_Disposed;

    public HierarchyLocalSelectionHandler(HierarchyView view, Unity.Hierarchy.Hierarchy hierarchy, IAuthoringSelection selection)
    {
        m_View = view ?? throw new ArgumentNullException(nameof(view));
        m_Hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        m_Selection = selection ?? throw new ArgumentNullException(nameof(selection));
        m_Selection.Changed += OnSelectionChanged;
    }

    public IAuthoringSelection Selection => m_Selection;

    /// <summary>
    /// A row that stands for another object in the selection, and that object: selecting the row selects the
    /// object, and selecting the object flags the row. Returns <see cref="HierarchyNode.Null"/> when there is none.
    /// </summary>
    public Func<(HierarchyNode Node, EntityId EntityId)> StandIn { get; set; }

    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        m_Selection.Changed -= OnSelectionChanged;
    }

    /// <summary>
    /// Writes the nodes the view has selected to the selection. Nothing is written when they already match, so a
    /// click on the selected row does not raise a change.
    /// </summary>
    public void SyncSelectionFromViewModel()
    {
        if (m_Disposed || !m_Hierarchy.IsCreated)
            return;

        var viewModel = m_View.ViewModel;
        var count = viewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
        var nodes = new HierarchyNode[count];
        viewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
        var entityIds = new EntityId[count];
        m_Hierarchy.GetEntityIdsFromNodes(nodes, entityIds);

        if (TryGetStandIn(out var standInNode, out var standInEntityId))
        {
            for (var i = 0; i < count; ++i)
            {
                if (nodes[i] == standInNode)
                    entityIds[i] = standInEntityId;
            }
        }

        using var _ = ListPool<EntityId>.Get(out var selected);
        foreach (var entityId in entityIds)
        {
            if (entityId != EntityId.None)
                selected.Add(entityId);
        }

        if (SameEntityIds(selected, m_Selection.EntityIds))
            return;

        m_WritingSelection = true;
        try
        {
            m_Selection.EntityIds = selected.ToArray();
        }
        finally
        {
            m_WritingSelection = false;
        }
    }

    /// <summary>
    /// Flags the nodes of the selected objects as selected in the view, and only those.
    /// </summary>
    public void SyncViewModelFromSelection(bool frameSelection)
    {
        m_SyncPending = false;
        if (m_Disposed || !m_Hierarchy.IsCreated)
            return;

        var entityIds = m_Selection.EntityIds;
        var nodes = new HierarchyNode[entityIds.Length];
        m_Hierarchy.GetNodesFromEntityIds(entityIds, nodes);

        if (TryGetStandIn(out var standInNode, out var standInEntityId))
        {
            for (var i = 0; i < entityIds.Length; ++i)
            {
                if (entityIds[i] == standInEntityId)
                    nodes[i] = standInNode;
            }
        }

        using var _ = ListPool<HierarchyNode>.Get(out var resolved);
        foreach (var node in nodes)
        {
            if (node != HierarchyNode.Null)
                resolved.Add(node);
        }

        var viewModel = m_View.ViewModel;
        viewModel.BeginFlagsChange();
        viewModel.ClearFlags(HierarchyNodeFlags.Selected);
        viewModel.SetFlags(resolved.ToArray(), HierarchyNodeFlags.Selected);
        viewModel.EndFlagsChangeWithoutNotify();

        if (frameSelection && resolved.Count > 0)
            m_View.Frame(resolved.ToArray());
    }

    void OnSelectionChanged()
    {
        // The selection raises its change synchronously, so a write of our own comes straight back here.
        if (m_WritingSelection || m_SyncPending || m_Disposed)
            return;

        m_SyncPending = true;
        m_View.EnqueuePostUpdateAction(() => SyncViewModelFromSelection(frameSelection: true));
    }

    bool TryGetStandIn(out HierarchyNode node, out EntityId entityId)
    {
        (node, entityId) = StandIn?.Invoke() ?? (HierarchyNode.Null, EntityId.None);
        return node != HierarchyNode.Null && entityId != EntityId.None;
    }

    static bool SameEntityIds(List<EntityId> lhs, EntityId[] rhs)
    {
        if (lhs.Count != rhs.Length)
            return false;

        for (var i = 0; i < rhs.Length; ++i)
        {
            if (lhs[i] != rhs[i])
                return false;
        }

        return true;
    }
}
