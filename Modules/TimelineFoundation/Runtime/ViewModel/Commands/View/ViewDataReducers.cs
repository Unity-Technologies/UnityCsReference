// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.Commands.ViewData
{
    static class Reducers
    {
        public static void RegisterAll(ViewModelBase viewModel)
        {
            viewModel.RegisterCommandHandler<SequenceSourceComponent, ViewComponent, ResetViewData>(ResetViewDataReducer_Internal);
            viewModel.RegisterCommandHandler<ViewComponent, UpdateViewData>(UpdateViewDataReducer_Internal);
            viewModel.RegisterCommandHandler<ViewComponent, ChangeDisplayRange>(ChangeDisplayRangeReducer_Internal);
            viewModel.RegisterCommandHandler<ViewComponent, EncapsulateRange>(EncapsulateDisplayRangeReducer);
            viewModel.RegisterCommandHandler<ViewComponent, ChangeVerticalScrollOffset>(ChangeScrollOffsetReducer_Internal);
            viewModel.RegisterCommandHandler<ViewComponent, SetHeaderWidth>(SetHeaderWidthReducer_Internal);
        }

        internal static void ResetViewDataReducer_Internal(SequenceSourceComponent sequenceComponent, ViewComponent viewComponent, ResetViewData action)
        {
            ViewModel.Sequence sequence = sequenceComponent.readonlyData.sequence;
            DiscreteTime duration;
            if (sequence == null)
            {
                duration = new DiscreteTime(2);
            }
            else
            {
                duration = sequence.duration;
                if (duration == DiscreteTime.Zero)
                {
                    duration = new DiscreteTime(2);
                }
            }

            using (viewComponent.UpdateScope())
            {
                viewComponent.displayRange = new TimeRange(DiscreteTime.Zero, duration);
            }
        }

        internal static void UpdateViewDataReducer_Internal(ViewComponent viewComponent, UpdateViewData action)
        {
            using (viewComponent.UpdateScope())
            {
                viewComponent.CopyFrom(action.viewData);
            }
        }

        internal static void ChangeDisplayRangeReducer_Internal(ViewComponent viewComponent, ChangeDisplayRange action)
        {
            using (viewComponent.UpdateScope())
            {
                viewComponent.displayRange = action.displayRange;
            }
        }

        static void EncapsulateDisplayRangeReducer(ViewComponent viewComponent, EncapsulateRange action)
        {
            using (viewComponent.UpdateScope())
            {
                TimeRange range = viewComponent.displayRange;
                DiscreteTime min = DiscreteTimeTimeExtensions.Min(action.time, range.start);
                DiscreteTime max = DiscreteTimeTimeExtensions.Max(action.time, range.end);
                viewComponent.displayRange = new TimeRange(min, max);
            }
        }

        internal static void ChangeScrollOffsetReducer_Internal(ViewComponent viewComponent, ChangeVerticalScrollOffset action)
        {
            using (viewComponent.UpdateScope())
            {
                viewComponent.verticalScrollOffset = action.verticalScrollOffset;
            }
        }

        static void SetHeaderWidthReducer_Internal(ViewComponent viewComponent, SetHeaderWidth action)
        {
            using (viewComponent.UpdateScope())
            {
                viewComponent.headerWidth = action.width;
            }
        }
    }
}
