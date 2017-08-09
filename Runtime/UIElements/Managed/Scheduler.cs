// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    // All values are milliseconds
    // notes on precision:
    // the event will be fired no sooner than delay, and repeat no sooner than interval
    // this means that depending on system and application state the events could be fired less often or intervals skipped entirely.
    // it is the registrar's responsibility to read the TimerState to determine the actual event timing
    // and make sure things like animation are smooth and time based.
    // a delay of 0 and interval of 0 will be interpreted as "as often as possible" this should be used sparingly and the work done should be very small
    public struct TimerState
    {
        public long start;
        public long now;

        public long deltaTime
        {
            get
            {
                return now - start;
            }
        }
    }

    // DSL aka fluent interface for scheduling
    public struct ScheduleBuilder
    {
        private readonly ScheduledItem m_ScheduledItem;

        internal ScheduleBuilder(ScheduledItem scheduledItem)
        {
            m_ScheduledItem = scheduledItem;
        }

        public ScheduleBuilder StartingIn(long delay)
        {
            if (m_ScheduledItem != null)
                m_ScheduledItem.delay = delay;
            return this;
        }

        public ScheduleBuilder Every(long interval)
        {
            if (m_ScheduledItem != null)
                m_ScheduledItem.interval = interval;
            return this;
        }

        public ScheduleBuilder Until(Func<bool> condition)
        {
            if (m_ScheduledItem != null)
                m_ScheduledItem.timerUpdateStopCondition += condition;
            return this;
        }
    }

    // the scheduler public interface
    public interface IScheduler
    {
        ScheduleBuilder Schedule(Action<TimerState> timerUpdateEvent, IEventHandler hanlder);
        // remove the event. It is very important to unregister when being removed from a Panel via OnLeavePanel()
        // an event that is never stopped will not be stopped until the panel is cleaned-up.
        void Unschedule(Action<TimerState> timerUpdateEvent);
    }

    internal class ScheduledItem
    {
        // delegate that takes a timer state and returns void
        public Action<TimerState> timerUpdateEvent;
        // delegate that returns a boolean
        public Func<bool> timerUpdateStopCondition;
        public IEventHandler handler;

        public long start { get; set; } // in milliseconds
        public long delay { get; set; } // in milliseconds
        public long interval { get; set; } // in milliseconds

        public ScheduledItem(Action<TimerState> timerUpdateEvent, IEventHandler handler)
        {
            this.timerUpdateEvent = timerUpdateEvent;
            this.handler = handler;
            this.start = (long)(Time.realtimeSinceStartup * 1000.0f); // current time
        }

        public bool IsUpdatable()
        {
            return (delay > 0 || interval > 0);
        }
    }

    // default scheduler implementation
    internal class TimerEventScheduler : IScheduler
    {
        private readonly List<ScheduledItem> m_ScheduledItems = new List<ScheduledItem>();

        private bool m_TransactionMode;
        private readonly List<ScheduledItem> m_ScheduleTansactions = new List<ScheduledItem>();
        private readonly List<Action<TimerState>> m_UnscheduleTransactions = new List<Action<TimerState>>();

        private void Schedule(ScheduledItem scheduleItem)
        {
            if (m_ScheduledItems.Contains(scheduleItem))
            {
                Debug.LogError("Cannot schedule function " + scheduleItem.timerUpdateEvent + " more than once");
            }
            else
            {
                m_ScheduledItems.Add(scheduleItem);
            }
        }

        public ScheduleBuilder Schedule(Action<TimerState> timerUpdateEvent, IEventHandler handler)
        {
            var scheduleItem = new ScheduledItem(timerUpdateEvent, handler);

            if (m_TransactionMode)
            {
                m_ScheduleTansactions.Add(scheduleItem);
            }
            else
            {
                Schedule(scheduleItem);
            }

            return new ScheduleBuilder(scheduleItem);
        }

        public void Unschedule(Action<TimerState> timerUpdateEvent)
        {
            if (m_TransactionMode)
            {
                m_UnscheduleTransactions.Add(timerUpdateEvent);
                return;
            }

            var item = m_ScheduledItems.Find(t => t.timerUpdateEvent == timerUpdateEvent);

            if (item != null)
            {
                m_ScheduledItems.Remove(item);
            }
            else
            {
                Debug.LogError("Cannot unschedule unknown scheduled function " + timerUpdateEvent);
            }
        }

        public void UpdateScheduledEvents()
        {
            try
            {
                m_TransactionMode = true;

                // TODO: On a GAME Panel game time should be per frame and not change during a frame.
                // TODO: On an Editor Panel time should be real time
                long currentTime = (long)(Time.realtimeSinceStartup * 1000.0f);

                for (int i = 0; i < m_ScheduledItems.Count; i++)
                {
                    ScheduledItem scheduledItem = m_ScheduledItems[i];

                    VisualElement handlerAsVisualElement = scheduledItem.handler as VisualElement;
                    if (handlerAsVisualElement != null && handlerAsVisualElement.panel == null)
                    {
                        // Seems we have an orphan timer event, unschedule it immediately and don't even try to execute
                        Debug.Log("Will unschedule action of " + scheduledItem.handler + " because it has no panel");
                        Unschedule(scheduledItem.timerUpdateEvent);
                        continue;
                    }

                    if (!scheduledItem.IsUpdatable())
                    {
                        continue;
                    }

                    TimerState timerState = new TimerState {start = scheduledItem.start, now = currentTime};
                    if (currentTime - scheduledItem.delay > scheduledItem.start)
                    {
                        if (scheduledItem.timerUpdateEvent != null)
                        {
                            scheduledItem.timerUpdateEvent(timerState);
                        }
                        scheduledItem.start = currentTime;
                        scheduledItem.delay = scheduledItem.interval;

                        if (scheduledItem.timerUpdateStopCondition != null && scheduledItem.timerUpdateStopCondition())
                        {
                            Unschedule(scheduledItem.timerUpdateEvent);
                        }
                    }
                }
            }
            finally
            {
                m_TransactionMode = false;

                // Rule: remove unscheduled transactions first
                for (int s = 0; s < m_UnscheduleTransactions.Count; s++)
                {
                    Unschedule(m_UnscheduleTransactions[s]);
                }
                m_UnscheduleTransactions.Clear();

                // Then add scheduled transactions
                for (int s = 0; s < m_ScheduleTansactions.Count; s++)
                {
                    Schedule(m_ScheduleTansactions[s]);
                }
                m_ScheduleTansactions.Clear();
            }
        }
    }
}
