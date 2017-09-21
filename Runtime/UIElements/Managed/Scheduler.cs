// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    // All values are milliseconds
    // notes on precision:
    // the event will be fired no sooner than delayMs, and repeat no sooner than intervalMs
    // this means that depending on system and application state the events could be fired less often or intervals skipped entirely.
    // it is the registrar's responsibility to read the TimerState to determine the actual event timing
    // and make sure things like animation are smooth and time based.
    // a delayMs of 0 and intervalMs of 0 will be interpreted as "as often as possible" this should be used sparingly and the work done should be very small
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


    public interface IScheduledItem
    {
    }


    // the scheduler public interface
    public interface IScheduler
    {
        IScheduledItem ScheduleOnce(Action<TimerState> timerUpdateEvent, long delayMs);
        IScheduledItem ScheduleUntil(Action<TimerState> timerUpdateEvent, long delayMs, long intervalMs, Func<bool> stopCondition = null);
        IScheduledItem ScheduleForDuration(Action<TimerState> timerUpdateEvent, long delayMs, long intervalMs, long durationMs);

        // removes the event.
        // an event that is never stopped will not be stopped until the panel is cleaned-up.
        void Unschedule(IScheduledItem item);

        void Schedule(IScheduledItem item);
    }


    internal abstract class ScheduledItem : IScheduledItem
    {
        // delegate that returns a boolean
        public Func<bool> timerUpdateStopCondition;

        public static readonly Func<bool> OnceCondition = () => true;
        public static readonly Func<bool> ForeverCondition = () => false;

        public long startMs { get; set; }
        public long delayMs { get; set; }
        public long intervalMs { get; set; }

        public long endTimeMs { get; private set; }

        public ScheduledItem()
        {
            ResetStartTime();
            timerUpdateStopCondition = OnceCondition;
        }

        protected void ResetStartTime()
        {
            this.startMs = (long)(Time.realtimeSinceStartup * 1000.0f); // current time
        }

        public void SetDuration(long durationMs)
        {
            endTimeMs = startMs + durationMs;
        }

        public abstract void PerformTimerUpdate(TimerState state);

        internal virtual void OnItemUnscheduled()
        {}

        public virtual bool ShouldUnschedule()
        {
            if (endTimeMs > 0)
            {
                if (Time.realtimeSinceStartup * 1000.0f > endTimeMs)
                {
                    return true;
                }
            }

            if (timerUpdateStopCondition != null)
            {
                return timerUpdateStopCondition();
            }

            return false;
        }
    }

    // default scheduler implementation
    internal class TimerEventScheduler : IScheduler
    {
        private readonly List<ScheduledItem> m_ScheduledItems = new List<ScheduledItem>();

        private bool m_TransactionMode;
        private readonly List<ScheduledItem> m_ScheduleTransactions = new List<ScheduledItem>();
        private readonly List<ScheduledItem> m_UnscheduleTransactions = new List<ScheduledItem>();

        private int m_LastUpdatedIndex = -1;
        private class TimerEventSchedulerItem : ScheduledItem
        {
            // delegate that takes a timer state and returns void
            private readonly Action<TimerState> m_TimerUpdateEvent;

            public TimerEventSchedulerItem(Action<TimerState> updateEvent)
            {
                m_TimerUpdateEvent = updateEvent;
            }

            public override void PerformTimerUpdate(TimerState state)
            {
                if (m_TimerUpdateEvent != null)
                {
                    m_TimerUpdateEvent(state);
                }
            }

            public override string ToString()
            {
                return m_TimerUpdateEvent.ToString();
            }
        }

        public void Schedule(IScheduledItem item)
        {
            if (item == null)
                return;

            ScheduledItem scheduledItem = item as ScheduledItem;

            if (scheduledItem == null)
            {
                throw new NotSupportedException("Scheduled Item type is not supported by this scheduler");
            }

            if (m_TransactionMode)
            {
                m_ScheduleTransactions.Add(scheduledItem);
            }
            else
            {
                if (m_ScheduledItems.Contains(scheduledItem))
                {
                    throw new ArgumentException(string.Concat("Cannot schedule function ", scheduledItem, " more than once"));
                }
                else
                {
                    m_ScheduledItems.Add(scheduledItem);
                }
            }
        }

        public IScheduledItem ScheduleOnce(Action<TimerState> timerUpdateEvent, long delayMs)
        {
            var scheduleItem = new TimerEventSchedulerItem(timerUpdateEvent)
            {
                delayMs = delayMs
            };

            Schedule(scheduleItem);

            return scheduleItem;
        }

        public IScheduledItem ScheduleUntil(Action<TimerState> timerUpdateEvent, long delayMs, long intervalMs,
            Func<bool> stopCondition)
        {
            var scheduleItem = new TimerEventSchedulerItem(timerUpdateEvent)
            {
                delayMs = delayMs,
                intervalMs = intervalMs,
                timerUpdateStopCondition = stopCondition
            };

            Schedule(scheduleItem);
            return scheduleItem;
        }

        public IScheduledItem ScheduleForDuration(Action<TimerState> timerUpdateEvent, long delayMs, long intervalMs,
            long durationMs)
        {
            var scheduleItem = new TimerEventSchedulerItem(timerUpdateEvent)
            {
                delayMs = delayMs,
                intervalMs = intervalMs,
                timerUpdateStopCondition = null
            };

            scheduleItem.SetDuration(durationMs);

            Schedule(scheduleItem);
            return scheduleItem;
        }

        private bool RemovedScheduledItemAt(int index)
        {
            if (index >= 0)
            {
                var item = m_ScheduledItems[index];
                m_ScheduledItems.RemoveAt(index);
                item.OnItemUnscheduled();

                return true;
            }
            return false;
        }

        public void Unschedule(IScheduledItem item)
        {
            ScheduledItem sItem = item as ScheduledItem;
            if (sItem != null)
            {
                if (m_TransactionMode)
                {
                    m_UnscheduleTransactions.Add(sItem);   //TODO: optimize this, we lose the item and need to re-search
                }
                else
                {
                    if (!RemovedScheduledItemAt(m_ScheduledItems.IndexOf(sItem)))
                    {
                        throw new ArgumentException("Cannot unschedule unknown scheduled function " + sItem);
                    }
                }
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

                int itemsCount = m_ScheduledItems.Count;

                const long maxMsPerUpdate = 20;
                long maxTime = currentTime + maxMsPerUpdate;

                int startIndex = m_LastUpdatedIndex + 1;
                if (startIndex >= itemsCount)
                    startIndex = 0;


                for (int i = 0; i < itemsCount; i++)
                {
                    currentTime = (long)(Time.realtimeSinceStartup * 1000.0f);

                    if (currentTime >= maxTime)
                    {
                        //We spent too much time on this frame updating items, we break for now, we'll resume next frame
                        break;
                    }
                    int index = startIndex + i;
                    if (index >= itemsCount)
                    {
                        index -= itemsCount;
                    }

                    ScheduledItem scheduledItem = m_ScheduledItems[index];

                    if (currentTime - scheduledItem.delayMs >= scheduledItem.startMs)
                    {
                        TimerState timerState = new TimerState { start = scheduledItem.startMs, now = currentTime };

                        scheduledItem.PerformTimerUpdate(timerState);

                        scheduledItem.startMs = currentTime;
                        scheduledItem.delayMs = scheduledItem.intervalMs;

                        if (scheduledItem.ShouldUnschedule())
                        {
                            Unschedule(scheduledItem);
                        }
                    }

                    m_LastUpdatedIndex = index;
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
                for (int s = 0; s < m_ScheduleTransactions.Count; s++)
                {
                    Schedule(m_ScheduleTransactions[s]);
                }
                m_ScheduleTransactions.Clear();
            }
        }
    }
}
