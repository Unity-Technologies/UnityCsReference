// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Hierarchy.Editor
{
    sealed class HierarchyGlobalSelectionHandler : IDisposable
    {
        const int k_DefaultBufferSize = 1024;

        readonly HierarchyView m_HierarchyView;
        readonly EditorGUIUtility.EditorLockTracker m_LockTracker;
        HierarchyNode[] m_NodeBuffer = new HierarchyNode[k_DefaultBufferSize];
        EntityId[] m_EntityIdBuffer = new EntityId[k_DefaultBufferSize];
        bool m_SkipNextGlobalSelectionEvent;

        public HierarchyGlobalSelectionHandler(HierarchyView view, EditorGUIUtility.EditorLockTracker lockTracker)
        {
            m_HierarchyView = view;
            m_LockTracker = lockTracker;
            Selection.selectionChanged += OnGlobalSelectionChanged;
            m_HierarchyView.OnFlagsChanged += OnHierarchyViewFlagsChanged;
        }

        public void Dispose()
        {
            m_HierarchyView.OnFlagsChanged -= OnHierarchyViewFlagsChanged;
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

            // Set the selection
            var nodes = FillNodeBufferFromEntityIds(globalSelection);
            using (var _ = new HierarchyViewModelFlagsChangeScope(m_HierarchyView.ViewModel))
            {
                m_HierarchyView.ViewModel.ClearFlags(HierarchyNodeFlags.Selected);
                m_HierarchyView.ViewModel.SetFlags(nodes, HierarchyNodeFlags.Selected);
            }

            // Frame the selection if requested
            if (frameSelection && !m_LockTracker.isLocked)
                m_HierarchyView.FrameNodes(nodes);
        }

        /// <summary>
        /// Synchronizes the global selection with the view model selection.
        /// </summary>
        public void SyncGlobalSelectionFromViewModel()
        {
            HierarchyLogging.Log("HierarchyGlobalSelectionHandler.SyncGlobalSelectionFromViewModel()");
            var viewModelSelection = FillEntityIdBufferFromViewModel();

            if (Selection.GetEntityIdsUnsafe().SequenceEqual(viewModelSelection))
                return;

            m_SkipNextGlobalSelectionEvent = true;
            Selection.SetEntityIdsUnsafe(viewModelSelection);
        }

        void OnHierarchyViewFlagsChanged(HierarchyViewFlagChangedEvent evt)
        {
            if (!evt.Flags.HasFlag(HierarchyNodeFlags.Selected))
                return;

            SyncGlobalSelectionFromViewModel();
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

        ReadOnlySpan<EntityId> FillEntityIdBufferFromViewModel()
        {
            var count = m_HierarchyView.ViewModel.HasAllFlagsCount(HierarchyNodeFlags.Selected);
            if (count == 0)
                return ReadOnlySpan<EntityId>.Empty;

            // Get all the selected nodes
            var nodes = GetNodeBuffer(count);
            m_HierarchyView.ViewModel.GetNodesWithAllFlags(HierarchyNodeFlags.Selected, nodes);

            // Convert the nodes to entity ids
            var entityIds = GetEntityIdBuffer(count);
            m_HierarchyView.Source.GetEntityIds(nodes, entityIds);

            return entityIds;
        }

        ReadOnlySpan<HierarchyNode> FillNodeBufferFromEntityIds(ReadOnlySpan<EntityId> entityIds)
        {
            // Convert the entity ids to nodes
            var nodes = GetNodeBuffer(entityIds.Length);
            m_HierarchyView.Source.GetNodes(entityIds, nodes);
            return nodes;
        }

        Span<HierarchyNode> GetNodeBuffer(int count)
        {
            if (m_NodeBuffer.Length < count)
                m_NodeBuffer = new HierarchyNode[count];
            return m_NodeBuffer.AsSpan(0, count);
        }

        Span<EntityId> GetEntityIdBuffer(int count)
        {
            if (m_EntityIdBuffer.Length < count)
                m_EntityIdBuffer = new EntityId[count];
            return m_EntityIdBuffer.AsSpan(0, count);
        }

        internal static class TestHelper
        {
            public static bool IsSkipNextGlobalSelectionEvent(HierarchyGlobalSelectionHandler handler) =>
                handler.m_SkipNextGlobalSelectionEvent;
        }
    }
}
