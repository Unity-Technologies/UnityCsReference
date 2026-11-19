// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Commands.Manipulations
{
    readonly struct MixInsert : ICommand
    {
        public readonly CutList cutList;
        public readonly CutList toInsert;
        public readonly Track track;
        public readonly IManipulationHandler handler;

        public MixInsert(CutList cutList, CutList toInsert, Track track, IManipulationHandler handler)
        {
            this.cutList = cutList;
            this.toInsert = toInsert;
            this.track = track;
            this.handler = handler;
        }
    }

    readonly struct RippleInsert : ICommand
    {
        public readonly CutList cutList;
        public readonly CutList toInsert;
        public readonly Track track;
        public readonly IManipulationHandler handler;

        public RippleInsert(CutList cutList, CutList toInsert, Track track, IManipulationHandler handler)
        {
            this.cutList = cutList;
            this.toInsert = toInsert;
            this.track = track;
            this.handler = handler;
        }
    }

    readonly struct InsertMarkers : ICommand
    {
        public readonly MarkerList destination;
        public readonly MarkerList toInsert;
        public readonly Track track;
        public readonly DiscreteTime delta;

        public InsertMarkers(MarkerList destination, MarkerList toInsert, Track track, DiscreteTime delta)
        {
            this.destination = destination;
            this.toInsert = toInsert;
            this.track = track;
            this.delta = delta;
        }
    }

    readonly struct RippleInsertMarkers : ICommand
    {
        public readonly MarkerList destination;
        public readonly MarkerList toInsert;
        public readonly Track track;
        public readonly DiscreteTime delta;

        public RippleInsertMarkers(MarkerList destination, MarkerList toInsert, Track track, DiscreteTime delta)
        {
            this.destination = destination;
            this.toInsert = toInsert;
            this.track = track;
            this.delta = delta;
        }
    }

    readonly struct RippleMove : ICommand
    {
        public readonly CutList cutList;
        public readonly Track track;
        public readonly DiscreteTime atTime;
        public readonly DiscreteTime delta;

        public RippleMove(Track track, CutList cutList, DiscreteTime atTime, DiscreteTime delta)
        {
            this.atTime = atTime;
            this.delta = delta;
            this.cutList = cutList;
            this.track = track;
        }
    }

    readonly struct ReplaceInsert : ICommand
    {
        public readonly CutList cutList;
        public readonly CutList toInsert;
        public readonly Track track;
        public readonly IManipulationHandler handler;

        public ReplaceInsert(CutList cutList, CutList toInsert, Track track, IManipulationHandler handler)
        {
            this.cutList = cutList;
            this.toInsert = toInsert;
            this.track = track;
            this.handler = handler;
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SetCurrentManipulation : ICommand
    {
        public readonly ManipulationState manipulationState;

        public SetCurrentManipulation(ManipulationState manipulationState)
        {
            this.manipulationState = manipulationState;
        }
    }
}
