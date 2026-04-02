// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.Commands.Manipulations
{
    static class MoveReducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            viewModel.RegisterCommandHandler<SequenceSourceComponent, SelectionComponent, MixInsert>(MixInsertReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, RippleMove>(RippleMoveReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, ReplaceInsert>(ReplaceInsertReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, RippleInsert>(RippleInsertReducer);
            viewModel.RegisterCommandHandler<ManipulationComponent, SetCurrentManipulation>(SetCurrentManipulationReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, InsertMarkers>(InsertMarkersReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, RippleInsertMarkers>(RippleInsertMarkersReducer);
        }

        public static void MixInsertReducer(SequenceSourceComponent sequenceComponent, SelectionComponent selectionComponent, MixInsert action)
        {
            var editor = new CutList.Editor(action.cutList);
            editor.InsertMix(action.toInsert, action.handler);

            using (sequenceComponent.UpdateScope())
            {
                action.track.model.SetCutList(editor.Finish());
            }

            using (selectionComponent.UpdateScope())
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                selectionComponent.Select(action.toInsert.Select(i => i.handle));
#pragma warning restore UA2001
            }
        }

        public static void RippleMoveReducer(SequenceSourceComponent sequenceComponent, RippleMove action)
        {
            using (sequenceComponent.UpdateScope())
            {
                CutList previousCutList = action.cutList;
                CutList.Iterator iterator = previousCutList.IteratorAtTime(action.atTime);

                var cutListEditor = new CutList.Editor(previousCutList);
                cutListEditor.RippleMove(iterator, action.delta);
                action.track.model.SetCutList(cutListEditor.Finish());
            }
        }

        public static void ReplaceInsertReducer(SequenceSourceComponent sequenceComponent, ReplaceInsert action)
        {
            var editor = new CutList.Editor(action.cutList);

            if (action.handler != null)
                editor.InsertReplace(action.toInsert, action.handler);
            else
                editor.InsertReplace(action.toInsert);

            CutList result = editor.Finish();

            using (sequenceComponent.UpdateScope())
            {
                ITrack trackModel = action.track.model;
                trackModel.SetCutList(result);
            }
        }

        public static void RippleInsertReducer(SequenceSourceComponent sequenceComponent, RippleInsert action)
        {
            var editor = new CutList.Editor(action.cutList);
            editor.RippleInsert(action.toInsert, action.handler);
            CutList result = editor.Finish();

            using (sequenceComponent.UpdateScope())
            {
                ITrack trackModel = action.track.model;
                trackModel.SetCutList(result);
            }
        }

        public static void InsertMarkersReducer(SequenceSourceComponent sequenceComponent, InsertMarkers action)
        {
            var result = new List<Marker>(action.destination.Count + action.toInsert.Count);
            result.AddRange(action.destination);
            result.InsertMarkers(action.toInsert, action.delta);

            using (sequenceComponent.UpdateScope())
            {
                ITrack trackModel = action.track.model;
                trackModel.SetMarkers(new MarkerList(result));
            }
        }

        public static void RippleInsertMarkersReducer(SequenceSourceComponent sequenceComponent, RippleInsertMarkers action)
        {
            MarkerList toInsert = action.toInsert;
            var result = new List<Marker>(action.destination.Count + toInsert.Count);
            result.AddRange(action.destination);

            TimeRange insertionRange = toInsert.GetEffectiveRange();
            result.RippleMarkers(insertionRange.start.NextValue(), action.delta)
                .InsertMarkers(toInsert, action.delta);

            using (sequenceComponent.UpdateScope())
            {
                ITrack trackModel = action.track.model;
                trackModel.SetMarkers(new MarkerList(result));
            }
        }

        static void SetCurrentManipulationReducer(ManipulationComponent manipulationComponent, SetCurrentManipulation action)
        {
            manipulationComponent?.SetManipulationState(action.manipulationState);
        }
    }
}
