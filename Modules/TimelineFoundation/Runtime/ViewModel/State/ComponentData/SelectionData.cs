// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    /// <summary>
    /// All the data required to react to Selection and Selection changes.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SelectionData : IReadOnlyData
    {
        public readonly bool valid;
        public readonly SelectionContainer selection;
        public readonly SelectionContainer newlySelected;
        public readonly SelectionContainer newlyDeselected;

        public SelectionData(SelectionContainer selection, SelectionContainer newlySelected, SelectionContainer newlyDeselected)
        {
            valid = true;
            this.selection = selection;
            this.newlySelected = newlySelected;
            this.newlyDeselected = newlyDeselected;
        }

        public IEnumerable<Track> GetSelectedTracks(SequenceData sequenceData)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return selection.tracks.Select(sequenceData.GetTrackFromId);
#pragma warning restore UA2001
        }

        public IEnumerable<Item> GetSelectedItems(SequenceData sequenceData)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return selection.clips.Concat(selection.markers).Select(sequenceData.GetItemFromId);
#pragma warning restore UA2001
        }
    }
}
