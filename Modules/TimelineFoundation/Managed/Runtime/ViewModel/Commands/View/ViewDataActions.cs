// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Commands.ViewData
{
    internal readonly struct ResetViewData : ICommand { }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct UpdateViewData : ICommand
    {
        public readonly ViewModel.ViewData viewData;

        public UpdateViewData(ViewModel.ViewData viewData)
        {
            this.viewData = viewData;
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ChangeDisplayRange : ICommand
    {
        public readonly TimeRange displayRange;

        public ChangeDisplayRange(TimeRange displayRange)
        {
            this.displayRange = displayRange;
        }
    }

    internal readonly struct EncapsulateRange : ICommand
    {
        public readonly DiscreteTime time;

        public EncapsulateRange(DiscreteTime time)
        {
            this.time = time;
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ChangeVerticalScrollOffset : ICommand
    {
        public readonly float verticalScrollOffset;

        public ChangeVerticalScrollOffset(float verticalScrollOffset)
        {
            this.verticalScrollOffset = verticalScrollOffset;
        }
    }

    internal readonly struct SetHeaderWidth : ICommand
    {
        public readonly float width;

        public SetHeaderWidth(float width)
        {
            this.width = width;
        }
    }
}
