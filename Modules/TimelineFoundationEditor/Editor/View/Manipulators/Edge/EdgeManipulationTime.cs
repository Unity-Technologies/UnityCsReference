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
    class EdgeManipulationTime
    {
        public DiscreteTime previewTime { get; private set; }
        public DiscreteTime manipulationTime { get; private set; }
        public DiscreteTime initialTime { get; }
        public TimeRange validRange
        {
            get => m_ValidRange == TimeRange.Empty ? TimeRange.MaxRange : m_ValidRange;
            set => m_ValidRange = value;
        }

        DiscreteTime currentTime { get; set; }
        DiscreteTime attractionDurationOffset { get; set; }

        TimeRange m_ValidRange = TimeRange.Empty;

        public EdgeManipulationTime(DiscreteTime manipulationTime)
        {
            initialTime = manipulationTime;
            currentTime = manipulationTime;
            previewTime = manipulationTime;
            this.manipulationTime = manipulationTime;
        }

        public void UpdateTime(DiscreteTime time)
        {
            manipulationTime = time.Clamp(validRange);
            previewTime = manipulationTime;

            currentTime = time;
            attractionDurationOffset = -(manipulationTime - currentTime).Abs();
        }

        public void SnapTimeToFrame(ICanvas canvas)
        {
            previewTime = canvas.timeConverter.RoundToFrame(currentTime).Clamp(validRange);
            attractionDurationOffset = (manipulationTime - previewTime).Abs() - (manipulationTime - currentTime).Abs();
        }

        public void SnapTimeToEdge(ICanvas canvas, SnapEngine snapEngine, float attractionWidth)
        {
            DiscreteTime attractionDuration = canvas.PixelWidthToDuration(attractionWidth) + attractionDurationOffset;
            SnapEngine.Result<DiscreteTime> snapResult = snapEngine.FindEdge(manipulationTime, attractionDuration);
            previewTime = snapResult.isSnapped && validRange.Intersects(snapResult.value) ? snapResult.value : previewTime;
        }
    }
}
