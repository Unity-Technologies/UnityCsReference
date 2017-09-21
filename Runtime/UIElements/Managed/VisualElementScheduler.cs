// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public interface IVisualElementScheduledItem
    {
        VisualElement element { get; }
        bool isActive { get; }

        void Resume();
        void Pause();

        //will reset the delay of this item
        // If the item is not currently scheduled, it will resume it
        void ExecuteLater(long delayMs);

        //Fluent interface to set parameters
        IVisualElementScheduledItem StartingIn(long delayMs);
        IVisualElementScheduledItem Every(long intervalMs);
        IVisualElementScheduledItem Until(Func<bool> stopCondition);
        IVisualElementScheduledItem ForDuration(long durationMs);
    }

    public interface IVisualElementScheduler
    {
        IVisualElementScheduledItem Execute(Action<TimerState> timerUpdateEvent);

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

    public partial class VisualElement : IVisualElementScheduler
    {
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
                isScheduled = true;
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
                isScheduled = true;
                ResetStartTime();
                element.elementPanel.scheduler.Schedule(this);
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
