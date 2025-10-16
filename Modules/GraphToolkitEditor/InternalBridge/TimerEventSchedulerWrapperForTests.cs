// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.InternalBridge
{
    // *** Only used in tests ***
    [Obsolete]  // TODO: fix this class to not use the obsolete Panel.TimeSinceStartup GTF-2219
    class TimerEventSchedulerWrapperForTests : IDisposable
    {
        readonly VisualElement m_VisualElement;

        public static TimerEventSchedulerWrapperForTests CreateTimerEventSchedulerWrapper(VisualElement graphView)
        {
            return new TimerEventSchedulerWrapperForTests(graphView);
        }

        TimerEventSchedulerWrapperForTests(VisualElement visualElement)
        {
            m_VisualElement = visualElement;
            Panel.TimeSinceStartup = () => TimeSinceStartup;
        }

        public long TimeSinceStartup { get; set; }

        public void Dispose()
        {
            Panel.TimeSinceStartup = null;
        }

        public void UpdateScheduledEvents()
        {
            TimerEventScheduler s = (TimerEventScheduler)m_VisualElement.elementPanel.scheduler;
            s.UpdateScheduledEvents();
        }

        public static void SetTimeSinceStartupCallback(Func<long> cb)
        {
            if (cb == null)
                Panel.TimeSinceStartup = null;
            else
                Panel.TimeSinceStartup = () => cb();
        }
    }
}
