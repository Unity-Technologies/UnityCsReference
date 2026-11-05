// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // All values are milliseconds
    // notes on precision:
    // the event will be fired no sooner than delayMs, and repeat no sooner than intervalMs
    // this means that depending on system and application state the events could be fired less often or intervals skipped entirely.
    // it is the registrar's responsibility to read the TimerState to determine the actual event timing
    // and make sure things like animation are smooth and time based.
    // a delayMs of 0 and intervalMs of 0 will be interpreted as "as often as possible" this should be used sparingly and the work done should be very small
    /// <summary>
    /// Contains timing information of [[IVisualElementScheduler]] events.
    /// </summary>
    public struct TimerState : IEquatable<TimerState>
    {
        /// <summary>
        /// Start time in milliseconds, or last callback time for repeatable <see cref="IVisualElementScheduledItem"/>.
        /// </summary>
        public long start { get; set; }
        /// <summary>
        /// Current time in milliseconds.
        /// </summary>
        public long now { get; set; }

        /// <summary>
        /// Time difference in milliseconds between <see cref="now"/> and the previous callback.
        /// </summary>
        public long deltaTime
        {
            get
            {
                return now - start;
            }
        }

        /// <summary>
        /// Compare this object with another object and return true if they are equal.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>True if the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is TimerState && Equals((TimerState)obj);
        }

        /// <summary>
        /// Compare this object with another object and return true if they are equal.
        /// </summary>
        /// <param name="other">The object to compare with.</param>
        /// <returns>True if the objects are equal.</returns>
        public bool Equals(TimerState other)
        {
            return start == other.start &&
                now == other.now &&
                deltaTime == other.deltaTime;
        }

        public override int GetHashCode()
        {
            var hashCode = 540054806;
            hashCode = hashCode * -1521134295 + start.GetHashCode();
            hashCode = hashCode * -1521134295 + now.GetHashCode();
            hashCode = hashCode * -1521134295 + deltaTime.GetHashCode();
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(TimerState state1, TimerState state2)
        {
            return state1.Equals(state2);
        }

        /// <undoc/>
        public static bool operator!=(TimerState state1, TimerState state2)
        {
            return !(state1 == state2);
        }
    }

    internal abstract partial class ScheduledItem
    {
        // delegate that returns a boolean
        public Func<bool> timerUpdateStopCondition;

        [NoAutoStaticsCleanup]
        public static readonly Func<bool> OnceCondition = () => true;
        [NoAutoStaticsCleanup]
        public static readonly Func<bool> ForeverCondition = () => false;

        public long startMs { get; set; }
        public long delayMs { get; set; }
        public long intervalMs { get; set; }

        public long endTimeMs { get; private set; }

        // A scheduler rather than a flag: an item pending removal from one panel's scheduler can already be on another.
        internal TimerEventScheduler containingScheduler;

        public ScheduledItem(long startMs)
        {
            ResetStartTime(startMs);
            timerUpdateStopCondition = OnceCondition;
        }

        protected void ResetStartTime(long startMs)
        {
            this.startMs = startMs;
        }

        public void SetDuration(long durationMs)
        {
            endTimeMs = startMs + durationMs;
        }

        public void OffsetBy(long deltaMs)
        {
            if (endTimeMs > 0)
            {
                endTimeMs += deltaMs;
            }
            startMs += deltaMs;
        }
        public abstract void PerformTimerUpdate(TimerState state);

        internal virtual void OnItemUnscheduled()
        {}

        public virtual bool ShouldUnschedule()
        {
            if (timerUpdateStopCondition != null)
            {
                return timerUpdateStopCondition();
            }

            return false;
        }
    }

    // default scheduler implementation
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class TimerEventScheduler
    {
        private readonly List<ScheduledItem> m_ScheduledItems = new List<ScheduledItem>();

        private bool m_TransactionMode;
        private readonly List<ScheduledItem> m_ScheduleTransactions = new List<ScheduledItem>(); // order is important schedules are executed in add order
        private readonly HashSet<ScheduledItem> m_UnscheduleTransactions = new HashSet<ScheduledItem>(); // order no important. removal from m_ScheduledItems has no side effect

        internal bool disableThrottling = false;

        private int m_LastUpdatedIndex = -1;

        private BaseVisualElementPanel panel;
        public TimerEventScheduler(BaseVisualElementPanel p)
        {
            panel = p;
        }

        private class TimerEventSchedulerItem : ScheduledItem
        {
            // delegate that takes a timer state and returns void
            private readonly Action<TimerState> m_TimerUpdateEvent;

            public TimerEventSchedulerItem(long startMs, Action<TimerState> updateEvent) : base(startMs)
            {
                m_TimerUpdateEvent = updateEvent;
            }

            public override void PerformTimerUpdate(TimerState state)
            {
                m_TimerUpdateEvent?.Invoke(state);
            }

            public override string ToString()
            {
                return m_TimerUpdateEvent.ToString();
            }
        }

        public void Schedule(ScheduledItem item)
        {
            if (item == null)
                return;

            ScheduledItem scheduledItem = item;

            if (scheduledItem == null)
            {
                throw new NotSupportedException("Scheduled Item type is not supported by this scheduler");
            }

            if (m_TransactionMode)
            {
                if (m_UnscheduleTransactions.Remove(scheduledItem))
                {
                    // The item was unscheduled then rescheduled in the same transaction.
                    scheduledItem.containingScheduler = this;   // another scheduler may have taken and released it meanwhile
                }
                else if (IsInScheduledItems(scheduledItem) || m_ScheduleTransactions.Contains(scheduledItem))
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
                if (IsInScheduledItems(scheduledItem))
                {
                    throw new ArgumentException(string.Concat("Cannot schedule function ", scheduledItem, " more than once"));
                }
                else
                {
                    m_ScheduledItems.Add(scheduledItem);
                    scheduledItem.containingScheduler = this;
                }
            }
        }

        public ScheduledItem ScheduleOnce(Action<TimerState> timerUpdateEvent, long delayMs)
        {
            var scheduleItem = new TimerEventSchedulerItem(panel.TimeSinceStartupMs(), timerUpdateEvent)
            {
                delayMs = delayMs
            };

            Schedule(scheduleItem);

            return scheduleItem;
        }

        public ScheduledItem  ScheduleUntil(Action<TimerState> timerUpdateEvent, long delayMs, long intervalMs,
            Func<bool> stopCondition)
        {
            var scheduleItem = new TimerEventSchedulerItem(panel.TimeSinceStartupMs(), timerUpdateEvent)
            {
                delayMs = delayMs,
                intervalMs = intervalMs,
                timerUpdateStopCondition = stopCondition
            };

            Schedule(scheduleItem);
            return scheduleItem;
        }

        public ScheduledItem ScheduleForDuration(Action<TimerState> timerUpdateEvent, long delayMs, long intervalMs,
            long durationMs)
        {
            var scheduleItem = new TimerEventSchedulerItem(panel.TimeSinceStartupMs(), timerUpdateEvent)
            {
                delayMs = delayMs,
                intervalMs = intervalMs,
                timerUpdateStopCondition = null
            };

            scheduleItem.SetDuration(durationMs);

            Schedule(scheduleItem);
            return scheduleItem;
        }

        private bool IsInScheduledItems(ScheduledItem item)
        {
            return item.containingScheduler == this;
        }

        private void ReleaseItem(ScheduledItem item)
        {
            if (item.containingScheduler == this)
                item.containingScheduler = null;
        }

        private bool RemovedScheduledItemAt(int index)
        {
            if (index >= 0)
            {
                if (index <= m_LastUpdatedIndex) --m_LastUpdatedIndex;
                ReleaseItem(m_ScheduledItems[index]);
                m_ScheduledItems.RemoveAt(index);

                return true;
            }
            return false;
        }

        public void Unschedule(ScheduledItem item)
        {
            ScheduledItem sItem = item;
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
                    else if (IsInScheduledItems(sItem))
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

        public void AdjustCurrentTime(double previousCurrentTime, double newCurrentTime)
        {
            if (m_ScheduleTransactions.Count > 0 || m_UnscheduleTransactions.Count > 0)
            {
                throw new InvalidOperationException("Adjusting time cannot be done while the scheduler is being updated");
            }

            long deltaMs = (long)((newCurrentTime - previousCurrentTime) * 1000.0);

            for (int i = 0; i < m_ScheduledItems.Count; ++i)
            {
                m_ScheduledItems[i].OffsetBy(deltaMs);
            }
        }

        bool PrivateUnSchedule(ScheduledItem sItem)
        {
            return m_ScheduleTransactions.Remove(sItem) || RemovedScheduledItemAt(m_ScheduledItems.IndexOf(sItem));
        }

        private void RemoveUnscheduledTransactionsFromScheduledItems()
        {
            int lastUpdatedIndex = m_LastUpdatedIndex;
            int write = 0;
            for (int read = 0; read < m_ScheduledItems.Count; ++read)
            {
                var item = m_ScheduledItems[read];
                if (m_UnscheduleTransactions.Contains(item))
                {
                    ReleaseItem(item);
                    if (read <= lastUpdatedIndex)
                        --m_LastUpdatedIndex;
                    continue;
                }

                m_ScheduledItems[write++] = item;
            }

            m_ScheduledItems.RemoveRange(write, m_ScheduledItems.Count - write);
        }

        // For UI Test Framework.
        public long FrameCount
        {
            get { return frameCount; }
            set { frameCount = value; }
        }
        private long frameCount;

        public void UpdateScheduledEvents()
        {
            bool incrementFrame = true;

            try
            {
                m_TransactionMode = true;

                long currentTime = panel.TimeSinceStartupMs();

                int itemsCount = m_ScheduledItems.Count;

                const long maxMsPerUpdate = 20;
                long maxTime = currentTime + maxMsPerUpdate;

                int startIndex = m_LastUpdatedIndex + 1;
                if (startIndex >= itemsCount)
                    startIndex = 0;

                for (int i = 0; i < itemsCount; i++)
                {
                    int index = startIndex + i;
                    if (index >= itemsCount)
                    {
                        index -= itemsCount;
                    }

                    ScheduledItem scheduledItem = m_ScheduledItems[index];

                    bool unscheduleItem = false;

                    bool isDue = currentTime - scheduledItem.delayMs >= scheduledItem.startMs;

                    if (isDue)
                    {
                        TimerState timerState = new TimerState { start = scheduledItem.startMs, now = currentTime };

                        if (m_UnscheduleTransactions.Count == 0 || !m_UnscheduleTransactions.Contains(scheduledItem)) // Don't execute items that have been marked for future removal
                        {
                            try
                            {
                                scheduledItem.PerformTimerUpdate(timerState);
                            }
                            catch (ExitGUIException)
                            {
                                throw;
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }

                        scheduledItem.startMs = currentTime;
                        scheduledItem.delayMs = scheduledItem.intervalMs;

                        if (scheduledItem.ShouldUnschedule())
                        {
                            unscheduleItem = true;
                        }
                    }

                    bool isExpired = scheduledItem.endTimeMs > 0 && currentTime > scheduledItem.endTimeMs;

                    if (unscheduleItem || isExpired)
                    {
                        // if the scheduledItem has been unscheduled explicitly in PerformTimerUpdate then
                        // it will be in m_UnscheduleTransactions and we shouldn't unschedule it again
                        if (m_UnscheduleTransactions.Count == 0 || !m_UnscheduleTransactions.Contains(scheduledItem))
                        {
                            Unschedule(scheduledItem);
                        }
                    }

                    m_LastUpdatedIndex = index;

                    if ((isDue || isExpired) && i + 1 < itemsCount)   // time only moves meaningfully after a callback or an unschedule
                    {
                        currentTime = panel.TimeSinceStartupMs();

                        if (!disableThrottling && currentTime >= maxTime)
                        {
                            //We spent too much time on this frame updating items, we break for now, we'll resume next frame
                            incrementFrame = false;
                            break;
                        }
                    }
                }
            }
            finally
            {
                m_TransactionMode = false;

                // Rule: remove unscheduled transactions first
                if (m_UnscheduleTransactions.Count > 1)
                {
                    RemoveUnscheduledTransactionsFromScheduledItems();
                }
                else
                {
                    foreach (var item in m_UnscheduleTransactions)
                    {
                        PrivateUnSchedule(item);
                    }
                }
                m_UnscheduleTransactions.Clear();

                // Then add scheduled transactions
                foreach (var item in m_ScheduleTransactions)
                {
                    Schedule(item);
                }
                m_ScheduleTransactions.Clear();

                if (incrementFrame)
                {
                    ++FrameCount;
                }
            }
        }
    }
}
