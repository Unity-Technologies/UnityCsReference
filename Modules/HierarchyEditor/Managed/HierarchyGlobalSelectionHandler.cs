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
    internal sealed partial class HierarchyGlobalSelectionHandler : IDisposable
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
            SyncViewModelFromGlobalSelectionNative(m_HierarchyView.ViewModel);
            if (frameSelection && !m_LockTracker.isLocked)
                FrameCurrentSelection();
        }

        /// <summary>
        /// Synchronizes the global selection with the view model selection.
        /// </summary>
        public void SyncGlobalSelectionFromViewModel()
        {
            HierarchyLogging.Log("HierarchyGlobalSelectionHandler.SyncGlobalSelectionFromViewModel()");
            m_SkipNextGlobalSelectionEvent = true;
            if (!SyncGlobalSelectionFromViewModelNative(m_HierarchyView.ViewModel))
                m_SkipNextGlobalSelectionEvent = false;
        }

        void FrameCurrentSelection()
        {
            var count = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            if (count == 0)
                return;
            using var nodes = new RentSpanUnmanaged<HierarchyNode>(count);
            m_HierarchyView.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
            m_HierarchyView.Frame(nodes);
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

            public static void ResetSkipNextGlobalSelectionEvent(HierarchyGlobalSelectionHandler handler) =>
                handler.m_SkipNextGlobalSelectionEvent = false;
        }
    }
}
