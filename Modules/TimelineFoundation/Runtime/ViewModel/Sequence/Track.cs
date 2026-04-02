// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class Track : Stack
    {
        public override UniqueID ID => model.ID;
        public ITrack model { get; }
        public Stack parent { get; private set; }

        CutList m_CutList;
        MarkerList m_MarkerList;
        IReadOnlyList<Item> m_ItemsList = new List<Item>();
        ITrackMetadata metadata { get; set; }

        public string name { get; private set; }

        public int index => (parent is Track t ? t.index + 1 : 0) + new List<Track>(parent.GetFlattenedChildren()).IndexOf(this);
        //TODO: ^ find a way to cache the index relative to parent or eliminate it (https://jira.unity3d.com/browse/ATL-1220)

        public IReadOnlyList<Item> Items => m_ItemsList;

        public Track(ITrack model, Stack parent)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            this.model = model;
            this.parent = parent;

            SetName_Internal(model.name);
            SetMetadata_Internal(model.metadata);
        }

        internal void SetMetadata_Internal(ITrackMetadata newMetadata)
        {
            metadata = newMetadata;
        }

        internal void SetName_Internal(string newName)
        {
            name = newName;
        }

        internal void SetItems_Internal(IReadOnlyList<Item> newItems)
        {
            m_ItemsList = newItems;
        }

        internal CutList GetCutList_Internal() => m_CutList;
        internal void SetCutList_Internal(CutList newCutList) => m_CutList = newCutList;

        internal MarkerList GetMarkerList_Internal() => m_MarkerList;
        internal void SetMarkerList_Internal(MarkerList newMarkerList) => m_MarkerList = newMarkerList;

        internal void SetParent_Internal(Track newParent)
        {
            parent = newParent;
        }

        public override string ToString()
        {
            return model.ToString();
        }

        public ITrackMetadata GetGenericMetadata()
        {
            return metadata;
        }

        public T GetMetadata<T>() where T : ITrackMetadata
        {
            switch (metadata)
            {
                case T specificTrackData:
                    return specificTrackData;
                case null:
                    throw new InvalidOperationException("Null track data.");
                default:
                    throw new InvalidOperationException($"Incorrect track data type. Actual: {metadata.GetType()} Expected:{typeof(T)}");
            }
        }
    }
}
