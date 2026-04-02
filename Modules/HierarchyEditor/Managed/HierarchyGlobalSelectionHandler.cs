// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace Unity.Hierarchy.Editor
{
    [VisibleToOtherModules]
    internal sealed class HierarchyGlobalSelectionHandler : IDisposable
    {
        readonly HierarchyView m_HierarchyView;
        readonly EditorGUIUtility.EditorLockTracker m_LockTracker;
        bool m_SkipNextGlobalSelectionEvent;

        public HierarchyGlobalSelectionHandler(HierarchyView view, EditorGUIUtility.EditorLockTracker lockTracker)
        {
            m_HierarchyView = view;
            m_LockTracker = lockTracker;
            Selection.selectionChanged += OnGlobalSelectionChanged;
        }

        public void Dispose()
        {
            Selection.selectionChanged -= OnGlobalSelectionChanged;
        }

        /// <summary>
        /// Synchronizes the view model selection with the global selection.
        /// </summary>
        /// <param name="frameSelection">Frame the selection after synchronizing.</param>
        public void SyncViewModelFromGlobalSelection(bool frameSelection)
        {
            HierarchyLogging.Log($"HierarchyGlobalSelectionHandler.SyncViewModelFromGlobalSelection(frameSelection={frameSelection})");
            var globalSelection = Selection.GetEntityIdsUnsafe();

            if (globalSelection.Length == 0)
            {
                m_HierarchyView.ViewModel.ClearFlags(HierarchyNodeFlags.Selected);
                return;
            }

            // Set the selection
            using var nodes = globalSelection.Length <= 16
                ? new RentSpanUnmanaged<HierarchyNode>(stackalloc HierarchyNode[globalSelection.Length])
                : new RentSpanUnmanaged<HierarchyNode>(globalSelection.Length);
            m_HierarchyView.Source.GetNodes(globalSelection, nodes);

            using (var _ = new HierarchyViewModelFlagsChangeScope(m_HierarchyView.ViewModel, notify: false))
            {
                m_HierarchyView.ViewModel.ClearFlags(HierarchyNodeFlags.Selected);
                m_HierarchyView.ViewModel.SetFlags(nodes, HierarchyNodeFlags.Selected);
            }

            // Frame the selection if requested
            if (frameSelection && !m_LockTracker.isLocked)
                m_HierarchyView.Frame(nodes);
        }

        /// <summary>
        /// Synchronizes the global selection with the view model selection.
        /// </summary>
        public void SyncGlobalSelectionFromViewModel()
        {
            HierarchyLogging.Log("HierarchyGlobalSelectionHandler.SyncGlobalSelectionFromViewModel()");
            var count = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            if (count == 0)
            {
                Selection.SetEntityIdsUnsafe(stackalloc EntityId[0]);
                return;
            }

            // Get all the selected nodes
            using var nodes = count <= 16
                ? new RentSpanUnmanaged<HierarchyNode>(stackalloc HierarchyNode[count])
                : new RentSpanUnmanaged<HierarchyNode>(count);
            m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);

            // Convert the nodes to entity ids
            using var selectedEntityIds = count <= 16
                ? new RentSpanUnmanaged<EntityId>(stackalloc EntityId[count])
                : new RentSpanUnmanaged<EntityId>(count);
            using var existingNodes = count <= 16
                ? new RentSpanUnmanaged<HierarchyNode>(stackalloc HierarchyNode[count])
                : new RentSpanUnmanaged<HierarchyNode>(count);
            var existingNodeCount = FilterMissingNodes(nodes, existingNodes);
            var trimmedNodes = existingNodes.Span[..existingNodeCount];
            var trimmedEntityIds = selectedEntityIds.Span[..existingNodeCount];
            m_HierarchyView.Source.GetEntityIds(trimmedNodes, trimmedEntityIds);

            if (Selection.GetEntityIdsUnsafe().SequenceEqual(trimmedEntityIds))
                return;

            m_SkipNextGlobalSelectionEvent = true;
            Selection.SetEntityIdsUnsafe(trimmedEntityIds);
        }

        int FilterMissingNodes(ReadOnlySpan<HierarchyNode> nodes, Span<HierarchyNode> existingNodes)
        {
            using var exists = nodes.Length <= 16
                ? new RentSpanUnmanaged<bool>(stackalloc bool[nodes.Length])
                : new RentSpanUnmanaged<bool>(nodes.Length);

            if (m_HierarchyView.Source.Exists(nodes, exists))
            {
                nodes.CopyTo(existingNodes);
                return nodes.Length;
            }

            var count = 0;
            for (var i = 0; i < nodes.Length; i++)
            {
                if (!exists.Span[i]) continue;

                existingNodes[count++] = nodes[i];
            }
            return count;
        }

        void OnGlobalSelectionChanged()
        {
            if (m_SkipNextGlobalSelectionEvent)
            {
                m_SkipNextGlobalSelectionEvent = false;
                return;
            }
            m_HierarchyView.EnqueuePostUpdateAction(() =>
            {
                SyncViewModelFromGlobalSelection(frameSelection: true);
            });
        }

        internal static class TestHelper
        {
            public static bool IsSkipNextGlobalSelectionEvent(HierarchyGlobalSelectionHandler handler) =>
                handler.m_SkipNextGlobalSelectionEvent;
        }
    }
}
