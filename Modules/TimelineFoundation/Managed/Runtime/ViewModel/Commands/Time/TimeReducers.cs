// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;

namespace Unity.Timeline.Foundation.Commands.Time
{
    static class Reducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            viewModel.RegisterCommandHandler<TimeComponent, SetDisplayTime>(SetGlobalTimeReducer);
            viewModel.RegisterCommandHandler<TimeComponent, SetLocalTime>(SetLocalTimeReducer_Internal);
            viewModel.RegisterCommandHandler<SequenceSourceComponent, TimeComponent, Step>(StepTimeReducer);
        }

        public static void SetGlobalTimeReducer(TimeComponent component, SetDisplayTime action)
        {
            component.displayTime = action.time;
        }

        internal static void SetLocalTimeReducer_Internal(TimeComponent component, SetLocalTime action)
        {
            component.localTime = action.time;
        }

        public static void StepTimeReducer(SequenceSourceComponent sequenceComponent, TimeComponent timeComponent, Step action)
        {
            ViewModel.Sequence sequence = sequenceComponent.readonlyData.sequence;
            if (sequence == null)
                return;

            FrameRate fps = sequence.frameRate;
            DiscreteTime currentTime = timeComponent.localTime;
            DiscreteTime oneFrame = new DiscreteTime(timeComponent.localToDisplayTimeTransform.multiplier / fps.rate);

            switch (action.direction)
            {
                case Step.Direction.Forward:
                    DiscreteTime nextFrame = currentTime + oneFrame;
                    timeComponent.localTime = nextFrame;
                    break;
                case Step.Direction.Backward:
                    DiscreteTime previousFrame = currentTime - oneFrame;
                    timeComponent.localTime = previousFrame;
                    break;
                case Step.Direction.None:
                    break;
            }
        }
    }
}
