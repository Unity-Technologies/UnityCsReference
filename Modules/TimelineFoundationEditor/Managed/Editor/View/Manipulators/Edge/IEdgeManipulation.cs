// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View
{
    interface IEdgeManipulation
    {
        public bool manipulationActive { get; }
        public ManipulationBehaviourOverlay overlay { get; }
        public Cursor GetCursor();
        public IReadOnlyList<Item> GetManipulatedItems();
        public TimeRange GetValidRange();

        public bool IsValid(Item target, EdgeHandle.Location location, EventModifiers modifiers);
        public void Begin(ISequenceViewModel viewModel, IManipulationHandler handler, Item target, EdgeHandle.Location location, out DiscreteTime edgeInitialTime);
        public void Process(DiscreteTime initialTime, DiscreteTime requestedTime, DiscreteTime rawTime);
        public void Apply();
        public void Cancel();
        public void End();
    }
}
