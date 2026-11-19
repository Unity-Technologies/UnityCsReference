// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.Model;

namespace Unity.Timeline.Foundation.ViewModel
{
    readonly struct MoveStateBundle
    {
        public readonly bool detached;
        public readonly MoveItemsState moveItemsState;
        public readonly MoveMarkersState moveMarkersState;
        public bool Invalid => !moveItemsState.shouldMoveItems && !moveMarkersState.shouldMoveMarkers;

        public MoveStateBundle(MoveItemsState moveItemsState, MoveMarkersState moveMarkersState, bool detached)
        {
            this.moveItemsState = moveItemsState;
            this.moveMarkersState = moveMarkersState;
            this.detached = detached;
        }
    }

    readonly struct MoveItemsState
    {
        public readonly bool shouldMoveItems;
        public readonly IReadOnlyList<CutList> movedItems;
        public readonly IReadOnlyList<CutList> itemsDestinations;

        public MoveItemsState(
            IReadOnlyList<CutList> movedItems,
            IReadOnlyList<CutList> itemsDestinations)
        {
            shouldMoveItems = true;
            this.movedItems = movedItems;
            this.itemsDestinations = itemsDestinations;
        }
    }

    readonly struct MoveMarkersState
    {
        public readonly bool shouldMoveMarkers;
        public readonly IReadOnlyList<MarkerList> movedMarkers;
        public readonly IReadOnlyList<MarkerList> markerDestinations;

        public MoveMarkersState(
            IReadOnlyList<MarkerList> movedMarkers,
            IReadOnlyList<MarkerList> markerDestinations)
        {
            shouldMoveMarkers = true;
            this.movedMarkers = movedMarkers;
            this.markerDestinations = markerDestinations;
        }
    }
}
