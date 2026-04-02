// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    sealed class SequenceTreeViewController
    {
        sealed class CellIdLookup
        {
            Dictionary<UniqueID, int> m_IdLookup = new();
            int m_LastTrackId = 0;

            public int GetCellID(Stack stack)
            {
                if (m_IdLookup.TryGetValue(stack.ID, out int id))
                    return id;

                int newId = ++m_LastTrackId;
                m_IdLookup[stack.ID] = newId;
                return newId;
            }

            public void RemoveCellID(UniqueID id) => m_IdLookup.Remove(id);

            public int GetCellID(UniqueID uniqueId)
            {
                return m_IdLookup.TryGetValue(uniqueId, out int id) ? id : -1;
            }
        }

        readonly ISequenceTreeView m_TreeView;
        readonly CellIdLookup m_CellIdLookup = new();
        Track[] m_HiddenTracks = Array.Empty<Track>();

        public SequenceTreeViewController(ISequenceTreeView treeView)
        {
            m_TreeView = treeView;
        }

        public void SetSequence(Sequence sequence)
        {
            IList<TreeViewItemData<Track>> treeViewTrackItems = BuildTrackItems(sequence.children);
            m_TreeView.SetItems(treeViewTrackItems);
            ApplyExpandedState(treeViewTrackItems);
            m_TreeView.Rebuild();
        }

        public void ApplySequenceDiff(SequenceDiff diff)
        {
            UpdateTracks(diff);
        }

        public void ApplySelectionData(SelectionData selectionData)
        {
            var selectedTrackIds = new List<int>();
            foreach (UniqueID trackUniqueID in selectionData.selection.tracks)
                selectedTrackIds.Add(m_CellIdLookup.GetCellID(trackUniqueID));

            m_TreeView.SetSelection(selectedTrackIds);
        }

        public int GetTreeViewId(UniqueID trackID)
        {
            return m_CellIdLookup.GetCellID(trackID);
        }

        public void SetHiddenTracks(params Track[] hiddenTracks)
        {
            m_HiddenTracks = hiddenTracks;
        }

        List<TreeViewItemData<Track>> BuildTrackItems(IEnumerable<Track> tracks)
        {
            var items = new List<TreeViewItemData<Track>>();

            foreach (Track track in tracks)
            {
                if (CanShowTrack(track))
                    items.Add(BuildTrackItem(track));
            }

            return items;
        }

        bool CanShowTrack(Track track)
        {
            foreach(var hiddenTrack in m_HiddenTracks)
                if (hiddenTrack.ID == track.ID)
                    return false;
            return true;
        }

        TreeViewItemData<Track> BuildTrackItem(Track track)
        {
            List<TreeViewItemData<Track>> children = BuildTrackItems(track.children);
            int cellID = m_CellIdLookup.GetCellID(track);
            return new TreeViewItemData<Track>(cellID, track, children);
        }

        void ApplyExpandedState(IEnumerable<TreeViewItemData<Track>> treeViewTrackItems)
        {
            foreach (TreeViewItemData<Track> treeViewTrackItem in treeViewTrackItems)
            {
                ApplyExpandedState(treeViewTrackItem.data);
                ApplyExpandedState(treeViewTrackItem.children);
            }
        }

        void ApplyExpandedState(Track track)
        {
            ITrackMetadata trackMetadata = track.GetGenericMetadata();
            if (trackMetadata != null)
            {
                int cellId = m_CellIdLookup.GetCellID(track);
                m_TreeView.SetExpanded(cellId, !trackMetadata.collapsed);
            }
        }

        void UpdateTracks(SequenceDiff diff)
        {
            ProcessTrackRemoved(diff.hierarchyChanges.removedTracks);

            var shouldRefresh = false;

            if (diff.hierarchyChanges.HasChanges())
            {
                m_TreeView.SetItems(BuildTrackItems(diff.sequence.children));
                shouldRefresh = true;
            }

            ProcessTrackChanges(diff.trackChanges, ref shouldRefresh);

            if (shouldRefresh)
                m_TreeView.Refresh();
        }

        void ProcessTrackRemoved(IEnumerable<Track> removedTracks)
        {
            foreach (Track track in removedTracks)
                m_CellIdLookup.RemoveCellID(track.ID);
        }

        void ProcessTrackChanges(IEnumerable<TrackChange> trackChanges, ref bool shouldRefresh)
        {
            foreach (TrackChange change in trackChanges)
            {
                if (change.MetadataHasChanged())
                {
                    ApplyExpandedState(change.track);
                    shouldRefresh = true;
                }
            }
        }
    }
}
