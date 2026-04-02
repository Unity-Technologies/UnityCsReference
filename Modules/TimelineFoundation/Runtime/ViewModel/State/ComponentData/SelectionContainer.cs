// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Timeline.Foundation.Common;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    /// <summary>
    /// Represents a single state of selection.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SelectionContainer
    {
        /// <summary>
        /// Empty Selection. Use this instead of allocating multiple empty selections
        /// </summary>
        public static SelectionContainer Empty = new(new List<UniqueID>(), new List<UniqueID>(), new List<UniqueID>(), new List<UniqueID>());

        public readonly ReadOnlyCollection<UniqueID> tracks;
        public readonly ReadOnlyCollection<UniqueID> clips;
        public readonly ReadOnlyCollection<UniqueID> markers;
        public readonly ReadOnlyCollection<UniqueID> transitions;

        /// <summary>
        /// Creates a new Selection Container.
        /// </summary>
        /// <remarks>
        /// Makes copies of the provided lists
        /// </remarks>
        /// <param name="tracks">Selected tracks</param>
        /// <param name="clips">Selected clips</param>
        /// <param name="markers">Selected markers</param>
        /// <param name="transitions">Selected transitions</param>
        public SelectionContainer(IEnumerable<UniqueID> tracks, IEnumerable<UniqueID> clips, IEnumerable<UniqueID> markers, IEnumerable<UniqueID> transitions)
        {
            this.tracks = new List<UniqueID>(tracks).AsReadOnly();
            this.clips = new List<UniqueID>(clips).AsReadOnly();
            this.markers = new List<UniqueID>(markers).AsReadOnly();
            this.transitions = new List<UniqueID>(transitions).AsReadOnly();
        }

        /// <summary>
        /// Returns true if given ID is in selection.
        /// </summary>
        /// <param name="id">UniqueID to find in selection.</param>
        public bool Contains(UniqueID id)
        {
            return tracks.Contains(id) || clips.Contains(id) || markers.Contains(id) || transitions.Contains(id);
        }

        /// <summary>
        /// Returns the number of items in the selection.
        /// </summary>
        public int Count()
        {
            return tracks.Count + clips.Count + markers.Count + transitions.Count;
        }
    }
}
