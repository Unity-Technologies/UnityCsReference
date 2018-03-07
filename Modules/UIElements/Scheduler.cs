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
            this.startMs = Panel.TimeSinceStartupMs();
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
                if (Panel.TimeSinceStartupMs() > endTimeMs)
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
        private readonly List<ScheduledItem> m_ScheduleTransactions = new List<ScheduledItem>(); // order is important schedules are executed in add order
        private readonly HashSet<ScheduledItem> m_UnscheduleTransactions = new HashSet<ScheduledItem>(); // order no important. removal from m_ScheduledItems has no side effect

        internal bool disableThrottling = false;

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
                if (m_UnscheduleTransactions.Remove(scheduledItem))
                {
                    // The item was unscheduled then rescheduled in the same transaction.
                }
                else if (m_ScheduledItems.Contains(scheduledItem) || m_ScheduleTransactions.Contains(scheduledItem))
                {
                    throw new ArgumentException(string.Concat("Cannot schedule function ", scheduledItem, " more than once"));
                }
                else
                {
                    m_ScheduleTransactions.Add(scheduledItem);
                }
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
                    if (m_UnscheduleTransactions.Contains(sItem))
                    {
                        throw new ArgumentException("Cannot unschedule scheduled function twice" + sItem);
                    }
                    else if (m_ScheduleTransactions.Remove(sItem))
                    {
                        // A item has been scheduled then unscheduled in the same transaction. which is valid.
                    }
                    else if (m_ScheduledItems.Contains(sItem))
                    {
                        // Only add it to m_UnscheduleTransactions if it is in m_ScheduledItems.
                        // if it was successfully removed from m_ScheduleTransaction we are fine.
                        m_UnscheduleTransactions.Add(sItem);
                    }
                    else
                    {
                        throw new ArgumentException("Cannot unschedule unknown scheduled function " + sItem);
                    }
                }
                else
                {
                    if (!PrivateUnSchedule(sItem))
                    {
                        throw new ArgumentException("Cannot unschedule unknown scheduled function " + sItem);
                    }
                }

                sItem.OnItemUnscheduled(); // Call OnItemUnscheduled immediately after successful removal even if we are in transaction mode
            }
        }

        bool PrivateUnSchedule(ScheduledItem sItem)
        {
            return m_ScheduleTransactions.Remove(sItem) || RemovedScheduledItemAt(m_ScheduledItems.IndexOf(sItem));
        }

        public void UpdateScheduledEvents()
        {
            try
            {
                m_TransactionMode = true;

                // TODO: On a GAME Panel game time should be per frame and not change during a frame.
                // TODO: On an Editor Panel time should be real time
                long currentTime = Panel.TimeSinceStartupMs();

                int itemsCount = m_ScheduledItems.Count;

                const long maxMsPerUpdate = 20;
                long maxTime = currentTime + maxMsPerUpdate;

                int startIndex = m_LastUpdatedIndex + 1;
                if (startIndex >= itemsCount)
                    startIndex = 0;


                for (int i = 0; i < itemsCount; i++)
                {
                    currentTime = Panel.TimeSinceStartupMs();

                    if (!disableThrottling && currentTime >= maxTime)
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

                        if (!m_UnscheduleTransactions.Contains(scheduledItem)) // Don't execute items that have been amrker for future removal
                            scheduledItem.PerformTimerUpdate(timerState);

                        scheduledItem.startMs = currentTime;
                        scheduledItem.delayMs = scheduledItem.intervalMs;

                        if (scheduledItem.ShouldUnschedule() && !m_UnscheduleTransactions.Contains(scheduledItem))
                        // if the scheduledItem has been unscheduled explicitly in PerformTimerUpdate then it will be in m_UnscheduleTransactions and we shouldn't
                        // unschedule it again
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
                foreach (var item in m_UnscheduleTransactions)
                {
                    PrivateUnSchedule(item);
                }
                m_UnscheduleTransactions.Clear();

                // Then add scheduled transactions
                foreach (var item in m_ScheduleTransactions)
                {
                    Schedule(item);
                }
                m_ScheduleTransactions.Clear();
            }
        }
    }
}
