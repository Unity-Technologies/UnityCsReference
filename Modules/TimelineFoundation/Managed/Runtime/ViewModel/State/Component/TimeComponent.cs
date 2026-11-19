// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel.Internals;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class TimeComponent : Component<TimeData>
    {
        public ITimeSource timeSource { get; }
        public bool displayIsGlobalTime { get; set; }
        public TimeTransform localToDisplayTimeTransform => displayIsGlobalTime ? m_TimeSourceData.timeTransform : TimeTransform.Identity;

        TimeSourceData m_TimeSourceData;

        public DiscreteTime displayTime
        {
            get => displayIsGlobalTime ? m_TimeSourceData.globalTime : m_TimeSourceData.localTime;
            set
            {
                TimeSourceData newTimeData = displayIsGlobalTime ? m_TimeSourceData.CopyWithGlobalTime(value) : m_TimeSourceData.CopyWithLocalTime(value);
                ApplyTimeSourceData(newTimeData);
            }
        }

        public DiscreteTime localTime
        {
            get => m_TimeSourceData.localTime;
            set => ApplyTimeSourceData(m_TimeSourceData.CopyWithLocalTime(value));
        }

        public TimeTransform localToGlobalTimeTransform
        {
            get => m_TimeSourceData.timeTransform;
            set => ApplyTimeSourceData(m_TimeSourceData.CopyWithTimeTransform(value));
        }

        public TimeComponent(ITimeSource source = null)
        {
            timeSource = source ?? new DefaultTimeSource();
            m_TimeSourceData = timeSource?.timeSourceData ?? TimeSourceData.Zero;
        }

        protected override TimeData GenerateReadOnlyData()
        {
            m_TimeSourceData = timeSource.timeSourceData;
            return new TimeData(m_TimeSourceData.timeTransform, displayTime, localToDisplayTimeTransform);
        }

        protected override void CheckForExternalChanges()
        {
            if (!timeSource.timeSourceData.Equals(m_TimeSourceData))
                MarkAsDirty();
        }

        void ApplyTimeSourceData(in TimeSourceData timeData)
        {
            timeSource.timeSourceData = timeData;
            m_TimeSourceData = timeData;
            MarkAsDirty();
        }
    }
}
