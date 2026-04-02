// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View.Internals
{
    class MoveManipulationTimeInfo
    {
        public TimeRange previewRange { get; private set; }
        public DiscreteTime lastDelta { get; private set; }
        public bool isSnappedLeft { get; private set; }
        public bool isSnappedRight { get; private set; }
        TimeRange manipulationRange { get; set; }
        DiscreteTime mouseTime { get; set; }
        DiscreteTime attractionDurationOffset { get; set; }

        public MoveManipulationTimeInfo(DiscreteTime startTime, TimeRange totalRange)
        {
            mouseTime = startTime;
            lastDelta = DiscreteTime.Zero;
            manipulationRange = previewRange = totalRange;
        }

        public void UpdatePreviewRange(DiscreteTime time, TimeRange validRange)
        {
            lastDelta = time - mouseTime;

            TimeRange newPreviewRange = manipulationRange + lastDelta;
            if (newPreviewRange.start < validRange.start)
                lastDelta = validRange.start - manipulationRange.start;

            isSnappedLeft = isSnappedRight = false;
            mouseTime += lastDelta;
            manipulationRange += lastDelta;
            previewRange = manipulationRange;
            attractionDurationOffset = DiscreteTime.Zero;
        }

        public void ApplySnapToFrame(ICanvas canvas)
        {
            previewRange = canvas.timeConverter.SnapToFrame(manipulationRange);
            attractionDurationOffset = (mouseTime - canvas.timeConverter.RoundToFrame(mouseTime)).Abs();
        }

        public void ApplyEdgeSnap(ICanvas canvas, SnapEngine snapEngine, float attractionWidth)
        {
            DiscreteTime attractionDuration = canvas.PixelWidthToDuration(attractionWidth) + attractionDurationOffset;
            SnapEngine.Result<TimeRange> snapResult = snapEngine.FindEdge(manipulationRange, attractionDuration);
            previewRange = snapResult.isSnapped ? snapResult.value : previewRange;

            isSnappedLeft = snapResult.location is SnapEngine.Location.Start or SnapEngine.Location.Both;
            isSnappedRight = snapResult.location is SnapEngine.Location.End or SnapEngine.Location.Both;
        }
    }
}
