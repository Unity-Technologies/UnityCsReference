using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a scheduled task created with a VisualElement's schedule interface.
    /// </summary>
    public interface IVisualElementScheduledItem
    {
        /// <summary>
        /// Returns the VisualElement this object is associated with.
        /// </summary>
        VisualElement element { get; }
        /// <summary>
        /// Will be true when this item is scheduled. Note that an item's callback will only be executed when it's VisualElement is attached to a panel.
        /// </summary>
        bool isActive { get; }

        /// <summary>
        /// If not already active, will schedule this item on its VisualElement's scheduler.
        /// </summary>
        void Resume();
        /// <summary>
        /// Removes this item from its VisualElement's scheduler.
        /// </summary>
        void Pause();

        //will reset the delay of this item
        // If the item is not currently scheduled, it will resume it
        /// <summary>
        /// Cancels any previously scheduled execution of this item and re-schedules the item.
        /// </summary>
        /// <param name="delayMs">Minimum time in milliseconds before this item will be executed.</param>
        void ExecuteLater(long delayMs);

        //Fluent interface to set parameters
        /// <summary>
        /// Adds a delay to the first invokation.
        /// </summary>
        /// <param name="delayMs">The minimum number of milliseconds after activation where this item's action will be executed.</param>
        /// <returns>This ScheduledItem.</returns>
        IVisualElementScheduledItem StartingIn(long delayMs);
        /// <summary>
        /// Repeats this action after a specified time.
        /// </summary>
        /// <param name="intervalMs">Minimum amount of time in milliseconds between each action execution.</param>
        /// <returns>This ScheduledItem.</returns>
        IVisualElementScheduledItem Every(long intervalMs);
        /// <summary>
        /// Item will be unscheduled automatically when specified condition is met.
        /// </summary>
        /// <param name="stopCondition">When condition returns true, the item will be unscheduled.</param>
        /// <returns>This ScheduledItem.</returns>
        IVisualElementScheduledItem Until(Func<bool> stopCondition);
        /// <summary>
        /// After specified duration, the item will be automatically unscheduled.
        /// </summary>
        /// <param name="durationMs">The total duration in milliseconds where this item will be active.</param>
        /// <returns>This ScheduledItem.</returns>
        IVisualElementScheduledItem ForDuration(long durationMs);
    }

    /// <summary>
    /// A scheduler allows you to register actions to be executed at a later point.
    /// </summary>
    public interface IVisualElementScheduler
    {
        /// <summary>
        /// Schedule this action to be executed later.
        /// </summary>
        /// <param name="timerUpdateEvent">The action to be executed.</param>
        /// <returns>Reference to the scheduled action.</returns>
        IVisualElementScheduledItem Execute(Action<TimerState> timerUpdateEvent);

        /// <summary>
        /// Schedule this action to be executed later.
        /// </summary>
        /// <param name="updateEvent">The action to be executed.</param>
        /// <returns>Reference to the scheduled action.</returns>
        IVisualElementScheduledItem Execute(Action updateEvent);
    }

    internal interface IVisualElementPanelActivatable
    {
        VisualElement element { get; }

        bool CanBeActivated();

        void OnPanelActivate();
        void OnPanelDeactivate();
    }

    internal class VisualElementPanelActivator
    {
        private IVisualElementPanelActivatable m_Activatable;
        public bool isActive { get; private set; }
        public bool isDetaching { get; private set; }

        public VisualElementPanelActivator(IVisualElementPanelActivatable activatable)
        {
            m_Activatable = activatable;
        }

        public void SetActive(bool action)
        {
            if (isActive != action)
            {
                isActive = action;
                if (isActive)
                {
                    m_Activatable.element.RegisterCallback<AttachToPanelEvent>(OnEnter);
                    m_Activatable.element.RegisterCallback<DetachFromPanelEvent>(OnLeave);
                    SendActivation();
                }
                else
                {
                    m_Activatable.element.UnregisterCallback<AttachToPanelEvent>(OnEnter);
                    m_Activatable.element.UnregisterCallback<DetachFromPanelEvent>(OnLeave);
                    SendDeactivation();
                }
            }
        }

        public void SendActivation()
        {
            if (m_Activatable.CanBeActivated())
            {
                m_Activatable.OnPanelActivate();
            }
        }

        public void SendDeactivation()
        {
            if (m_Activatable.CanBeActivated())
            {
                m_Activatable.OnPanelDeactivate();
            }
        }

        void OnEnter(AttachToPanelEvent evt)
        {
            if (isActive)
            {
                SendActivation();
            }
        }

        void OnLeave(DetachFromPanelEvent evt)
        {
            if (isActive)
            {
                isDetaching = true;
                try
                {
                    SendDeactivation();
                }
                finally
                {
                    isDetaching = false;
                }
            }
        }
    }

    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement : IVisualElementScheduler
    {
        /// <summary>
        /// Retrieves this VisualElement's IVisualElementScheduler
        /// </summary>
        public IVisualElementScheduler schedule { get { return this; } }

        /// <summary>
        /// This class is needed in order for its VisualElement to be notified when the scheduler removes an item
        /// </summary>
        private abstract class BaseVisualElementScheduledItem : ScheduledItem, IVisualElementScheduledItem, IVisualElementPanelActivatable
        {
            public VisualElement element { get; private set; }

            public bool isActive { get { return m_Activator.isActive; } }

            public bool isScheduled = false;


            private VisualElementPanelActivator m_Activator;
            protected BaseVisualElementScheduledItem(VisualElement handler)
            {
                element = handler;
                m_Activator = new VisualElementPanelActivator(this);
            }

            public IVisualElementScheduledItem StartingIn(long delayMs)
            {
                this.delayMs = delayMs;
                return this;
            }

            public IVisualElementScheduledItem Until(Func<bool> stopCondition)
            {
                if (stopCondition == null)
                {
                    stopCondition = ForeverCondition;
                }

                timerUpdateStopCondition = stopCondition;
                return this;
            }

            public IVisualElementScheduledItem ForDuration(long durationMs)
            {
                this.SetDuration(durationMs);
                return this;
            }

            public IVisualElementScheduledItem Every(long intervalMs)
            {
                this.intervalMs = intervalMs;

                if (timerUpdateStopCondition == OnceCondition)
                {
                    timerUpdateStopCondition = ForeverCondition;
                }
                return this;
            }

            internal override void OnItemUnscheduled()
            {
                base.OnItemUnscheduled();
                isScheduled = false;
                if (!m_Activator.isDetaching)
                {
                    m_Activator.SetActive(false);
                }
            }

            public void Resume()
            {
                m_Activator.SetActive(true);
            }

            public void Pause()
            {
                m_Activator.SetActive(false);
            }

            public void ExecuteLater(long delayMs)
            {
                if (!isScheduled)
                {
                    Resume();
                }
                ResetStartTime();
                StartingIn(delayMs);
            }

            public void OnPanelActivate()
            {
                if (!isScheduled)
                {
                    isScheduled = true;
                    ResetStartTime();
                    element.elementPanel.scheduler.Schedule(this);
                }
            }

            public void OnPanelDeactivate()
            {
                if (isScheduled)
                {
                    isScheduled = false;
                    element.elementPanel.scheduler.Unschedule(this);
                }
            }

            public bool CanBeActivated()
            {
                return element != null && element.elementPanel != null && element.elementPanel.scheduler != null;
            }
        }

        /// <summary>
        /// We invoke updateEvents in subclasses to avoid lambda wrapping allocations
        /// </summary>
        private abstract class VisualElementScheduledItem<ActionType> : BaseVisualElementScheduledItem
        {
            public ActionType updateEvent;
            public VisualElementScheduledItem(VisualElement handler, ActionType upEvent)
                : base(handler)
            {
                updateEvent = upEvent;
            }

            public static bool Matches(ScheduledItem item, ActionType updateEvent)
            {
                VisualElementScheduledItem<ActionType> vItem = item as VisualElementScheduledItem<ActionType>;

                if (vItem != null)
                {
                    return EqualityComparer<ActionType>.Default.Equals(vItem.updateEvent, updateEvent);
                }
                return false;
            }
        }

        private class TimerStateScheduledItem : VisualElementScheduledItem<Action<TimerState>>
        {
            public TimerStateScheduledItem(VisualElement handler, Action<TimerState> updateEvent)
                : base(handler, updateEvent)
            {
            }

            public override void PerformTimerUpdate(TimerState state)
            {
                if (isScheduled)
                {
                    updateEvent(state);
                }
            }
        }

        //we're doing this to avoid allocating a lambda for simple callbacks that don't need TimerState
        private class SimpleScheduledItem : VisualElementScheduledItem<Action>
        {
            public SimpleScheduledItem(VisualElement handler, Action updateEvent)
                : base(handler, updateEvent)
            {
            }

            public override void PerformTimerUpdate(TimerState state)
            {
                if (isScheduled)
                {
                    updateEvent();
                }
            }
        }

        IVisualElementScheduledItem IVisualElementScheduler.Execute(Action<TimerState> timerUpdateEvent)
        {
            var item = new TimerStateScheduledItem(this, timerUpdateEvent)
            {
                timerUpdateStopCondition = ScheduledItem.OnceCondition
            };
            item.Resume();
            return item;
        }

        IVisualElementScheduledItem IVisualElementScheduler.Execute(Action updateEvent)
        {
            var item = new SimpleScheduledItem(this, updateEvent)
            {
                timerUpdateStopCondition = ScheduledItem.OnceCondition
            };
            item.Resume();
            return item;
        }
    }
}
