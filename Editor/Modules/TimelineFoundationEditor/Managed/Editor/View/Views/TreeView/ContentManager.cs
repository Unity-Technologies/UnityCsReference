// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    sealed class ContentManager : IContentCreator
    {
        readonly Func<Track, ITrackHeaderElement> m_BuildTrackHeader;
        readonly Func<Track, ITrackContentElement> m_BuildTrackContent;
        readonly Func<Item, ItemElement> m_BuildItem;

        Dictionary<UniqueID, ITrackHeaderElement> m_TrackHeaderLookup = new();
        Dictionary<UniqueID, ITrackContentElement> m_TrackContentLookup = new();
        Dictionary<UniqueID, ItemElement> m_ItemElementLookup = new();

        public ContentManager(
            Func<Track, ITrackHeaderElement> buildTrackHeader,
            Func<Track, ITrackContentElement> buildTrackContent,
            Func<Item, ItemElement> buildItem)
        {
            m_BuildTrackHeader = buildTrackHeader;
            m_BuildTrackContent = buildTrackContent;
            m_BuildItem = buildItem;
        }

        public ContentLookup Lookup => new(m_TrackHeaderLookup, m_TrackContentLookup, m_ItemElementLookup);

        public IEnumerable<ItemElement> GetItemElements() => m_ItemElementLookup.Values;
        public ITrackHeaderElement GetTrackHeaderElement(UniqueID trackId) => m_TrackHeaderLookup.GetValue(trackId);
        public ITrackContentElement GetTrackElement(UniqueID trackId) => m_TrackContentLookup.GetValue(trackId);
        public ItemElement GetItemElement(UniqueID itemID) => m_ItemElementLookup.GetValue(itemID);

        public ITrackHeaderElement GetOrCreateTrackHeaderElement(Track track)
            => m_TrackHeaderLookup.GetValue(track.ID) ?? CreateTrackHeaderElement(track);

        public ITrackContentElement GetOrCreateTrackContentElement(Track track)
            => m_TrackContentLookup.GetValue(track.ID) ?? CreateTrackElement(track);

        public ITrackHeaderElement CreateTrackHeaderElement(Track track)
            => CreateTrackElement(track, m_BuildTrackHeader, m_TrackHeaderLookup);

        public ITrackContentElement CreateTrackElement(Track track)
        {
            ITrackContentElement trackElement = CreateTrackElement(track, m_BuildTrackContent, m_TrackContentLookup);
            CreateItemsForTrack_Internal(track);
            return trackElement;
        }

        TrackHeaderElement IContentCreator.CreateTrackHeaderElement(Track track)
            => CreateTrackHeaderElement(track) as TrackHeaderElement;

        TrackElement IContentCreator.CreateTrackElement(Track track)
            => CreateTrackElement(track) as TrackElement;

        public void ProcessSequenceChanges(SequenceDiff diff)
        {
            foreach (Track addedTrack in diff.hierarchyChanges.addedTracks)
                CreateItemsRecursive(addedTrack);

            foreach (Track removedTrack in diff.hierarchyChanges.removedTracks)
                DestroyItemsRecursive(removedTrack);

            foreach (TrackChange trackChange in diff.trackChanges)
                if (trackChange.ContentHasChanged())
                    UpdateItemsRecursive(trackChange.track);

            foreach (var item in m_ItemElementLookup.GetValues(diff.itemContentChanges))
                item.OnItemContentChanged();

            ProcessRemovedTracks(diff.hierarchyChanges.removedTracks);
            ProcessTrackChanges(diff.trackChanges);
            ProcessItemChanges(diff.itemContentChanges);
        }

        public void ProcessSelectionChanges(SelectionData data)
        {
            SetTrackSelectionState(data.newlySelected.tracks, true);
            SetTrackSelectionState(data.newlyDeselected.tracks, false);
            SetItemSelectionState(data.newlySelected.clips, true);
            SetItemSelectionState(data.newlyDeselected.clips, false);
            SetItemSelectionState(data.newlySelected.markers, true);
            SetItemSelectionState(data.newlyDeselected.markers, false);
        }

        void ProcessTrackChanges(IEnumerable<TrackChange> trackChanges)
        {
            foreach (TrackChange change in trackChanges)
            {
                if (m_TrackHeaderLookup.TryGetValue(change.track.ID, out ITrackHeaderElement headerElement))
                    UpdateTrackElement(change, headerElement);
                if (m_TrackContentLookup.TryGetValue(change.track.ID, out ITrackContentElement contentElement))
                    UpdateTrackElement(change, contentElement);
            }
        }

        void ProcessRemovedTracks(IEnumerable<Track> removedTracks)
        {
            foreach (Track track in removedTracks)
            {
                m_TrackHeaderLookup.Remove(track.ID);
                m_TrackContentLookup.Remove(track.ID);
            }
        }

        void ProcessItemChanges(IEnumerable<UniqueID> itemContentChanges)
        {
            foreach (ItemElement itemElement in m_ItemElementLookup.GetValues(itemContentChanges))
            {
                UniqueID parentID = itemElement.item.parent.ID;

                if (m_TrackHeaderLookup.TryGetValue(parentID, out ITrackHeaderElement headerElement))
                    headerElement.OnItemContentsChanged();
                if (m_TrackContentLookup.TryGetValue(parentID, out ITrackContentElement contentElement))
                    contentElement.OnItemContentsChanged();
            }
        }

        void SetTrackSelectionState(IEnumerable<UniqueID> trackIds, bool selected)
        {
            foreach (UniqueID trackId in trackIds)
            {
                if (m_TrackHeaderLookup.TryGetValue(trackId, out ITrackHeaderElement trackHeaderElement))
                    trackHeaderElement.OnSelectionStateChanged(selected);
                if (m_TrackContentLookup.TryGetValue(trackId, out ITrackContentElement trackElement))
                    trackElement.OnSelectionStateChanged(selected);
            }
        }

        void SetItemSelectionState(IEnumerable<UniqueID> itemIds, bool selected)
        {
            var items = m_ItemElementLookup.GetValues(itemIds);
            foreach (var item in items)
            {
                var selectableItem = item as ISelectableElement;
                if (selectableItem == null)
                    continue;

                selectableItem.OnSelectionStateChanged(selected);
            }
        }

        internal void CreateItemsForTrack_Internal(Track track)
        {
            ITrackContentElement trackElement = m_TrackContentLookup.GetValue(track.ID);
            if (trackElement != null) //track is not shown yet
            {
                SetupTrackItems(trackElement.GetItemsContainer(), track.Intervals());
                SetupTrackItems(trackElement.GetMarkersContainer(), track.Markers());
            }
        }

        internal void DestroyItemsFromTrack_Internal(Track track)
        {
            ITrackContentElement trackElement = m_TrackContentLookup.GetValue(track.ID);
            if (trackElement != null)
            {
                RemoveItemElements(trackElement.GetItemsContainer());
                RemoveItemElements(trackElement.GetMarkersContainer());
            }
        }

        internal void UpdateItemsForTrack_Internal(Track track)
        {
            ITrackContentElement trackElement = m_TrackContentLookup.GetValue(track.ID);
            if (trackElement != null)
            {
                UpdateItemsContainer(trackElement.GetItemsContainer(), track.Intervals());
                UpdateItemsContainer(trackElement.GetMarkersContainer(), track.Markers());
            }
        }

        void SetupTrackItems(VisualElement container, ItemView contents)
        {
            foreach (Item item in contents)
                CreateItemElement(container, item);
            CanvasUpdateEvent.Send(container, CanvasUpdateEvent.UpdateType.TargetAndDescendants);
        }

        void CreateItemsRecursive(Track track)
        {
            CreateItemsForTrack_Internal(track);
            foreach (Track child in track.children)
                CreateItemsRecursive(child);
        }

        void DestroyItemsRecursive(Track track)
        {
            foreach (Track child in track.children)
                DestroyItemsRecursive(child);
            DestroyItemsFromTrack_Internal(track);
        }

        void UpdateItemsRecursive(Track track)
        {
            foreach (Track child in track.children)
                UpdateItemsRecursive(child);
            UpdateItemsForTrack_Internal(track);
        }

        void UpdateItemsContainer(VisualElement container, ItemView contents)
        {
            var deletionCandidates = new HashSet<VisualElement>(container.Children());

            foreach (Item item in contents)
            {
                m_ItemElementLookup.TryGetValue(item.ID, out ItemElement itemElement);

                if (itemElement == null) //item element does not exist
                {
                    CreateItemElement(container, item);
                }
                else if (itemElement.parent == container) //item element is in the correct layer
                {
                    itemElement.SetItem(item);
                    deletionCandidates.Remove(itemElement);
                }
                else //item element is not in the correct layer
                {
                    RemoveItemElement(itemElement);
                    CreateItemElement(container, item);
                }
            }

            //remove item elements that are not in the track anymore
            foreach (VisualElement deletionCandidate in deletionCandidates)
            {
                if (deletionCandidate is ItemElement itemElement)
                    RemoveItemElement(itemElement);
            }

            CanvasUpdateEvent.Send(container, CanvasUpdateEvent.UpdateType.TargetAndDescendants);
        }

        void CreateItemElement(VisualElement container, Item item)
        {
            ItemElement itemElement = m_BuildItem(item);
            if (itemElement != null)
            {
                m_ItemElementLookup[itemElement.ID] = itemElement;
                container.Add(itemElement);
            }
        }

        void RemoveItemElement(ItemElement itemElement)
        {
            m_ItemElementLookup.Remove(itemElement.ID);
            itemElement.RemoveFromHierarchy();
        }

        void RemoveItemElements(VisualElement container)
        {
            foreach (VisualElement child in container.Children())
            {
                if (child is ItemElement itemElement)
                    m_ItemElementLookup.Remove(itemElement.ID);
            }
            container.Clear();
        }

        static void UpdateTrackElement(TrackChange change, ITrackElementNotification trackElement)
        {
            if (change.MetadataHasChanged())
                trackElement.OnTrackMetadataChanged();
            if (change.ContentHasChanged())
                trackElement.OnTrackContentsChanged();
        }

        static T CreateTrackElement<T>(Track track, Func<Track, T> build, Dictionary<UniqueID, T> lookup)
        {
            T trackElement = build(track);
            lookup[track.ID] = trackElement;
            return trackElement;
        }
    }
}
