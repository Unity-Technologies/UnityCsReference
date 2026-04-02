// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.Commands.Sequence
{
    static class Reducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            viewModel.RegisterCommandHandler<SequenceSourceComponent, SetDuration>(SetDurationReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, SetFrameRate>(SetFrameRateReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, CreateTrack>(CreateTrackReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, RemoveTrack>(RemoveTrackReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, ReparentTracks>(ReparentTracksReducer);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, SetTrackName>(SetTrackName);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, SetTrackContents>(SetTrackContentsReducer);
        }

        public static void SetDurationReducer(SequenceSourceComponent sequenceComponent, SetDuration command)
        {
            using (sequenceComponent.UpdateScope())
            {
                sequenceComponent.sequence.SetDuration(command.time);
            }
        }

        public static void SetFrameRateReducer(SequenceSourceComponent sequenceComponent, SetFrameRate command)
        {
            using (sequenceComponent.UpdateScope())
            {
                sequenceComponent.SetFrameRate(command.frameRate);
            }
        }

        public static void CreateTrackReducer(SequenceSourceComponent sequenceComponent, CreateTrack action)
        {
            using (sequenceComponent.UpdateScope())
            {
                ISequence model = sequenceComponent.sequence;
                model.CreateTrack(action.data);
            }
        }

        public static void RemoveTrackReducer(SequenceSourceComponent sequenceComponent, RemoveTrack action)
        {
            using (sequenceComponent.UpdateScope())
            {
                ISequence model = sequenceComponent.sequence;
                model.RemoveTrack(action.trackToRemove.model);
            }
        }

        public static void ReparentTracksReducer(SequenceSourceComponent sequenceComponent, ReparentTracks action)
        {
            using (sequenceComponent.UpdateScope())
            {
                sequenceComponent.sequence.ReparentTracks(action.data);
            }
        }

        public static void SetTrackName(SequenceSourceComponent sequenceComponent, SetTrackName action)
        {
            using (sequenceComponent.UpdateScope())
            {
                sequenceComponent.sequence.SetTrackName(action.target.model, action.name);
            }
        }

        public static void SetTrackContentsReducer(SequenceSourceComponent sequenceComponent, SetTrackContents action)
        {
            using (sequenceComponent.UpdateScope())
            {
                ITrack trackModel = action.track.model;
                if (action.newCutList != null)
                    trackModel.SetCutList(action.newCutList);
                if (action.newMarkerList != null)
                    trackModel.SetMarkers(action.newMarkerList);
            }
        }
    }
}
