// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    /// A scheduler provides the functionality to queue actions to run at a specific time or duration.
    /// </summary>
    /// <remarks>
    /// You can use the scheduler to create animations, update the UI, or create tasks that require a delay or repeated action.
    /// </remarks>
    /// <remarks>  
    /// To schedule an action, use the <see cref="IVisualElementScheduler.Execute"/> method. The scheduler runs the action at the next frame.
    /// </remarks>
    /// <remarks>  
    /// A <see cref="VisualElement"/> associates with the scheduler, which you can access through the <see cref="VisualElement.schedule"/> property.
    /// It returns an <see cref="IVisualElementScheduledItem"/> interface that provides methods to schedule the action.
    /// </remarks>
    /// <remarks>  
    /// For example, you can delay running of the action with the <see cref="IVisualElementScheduledItem.StartingIn"/> method. 
    /// You can also specify a condition to unschedule the action with the <see cref="IVisualElementScheduledItem.Until"/> method.
    /// </remarks>
    /// <remarks>  
    /// To repeat the action at a specified interval, use the <see cref="IVisualElementScheduledItem.Every"/> method.
    /// To remove the scheduler, use the <see cref="IVisualElementScheduledItem.Pause"/> method.
    /// </remarks>
    /// <remarks>
    /// The scheduler is active only when the VisualElement is attached to a panel. Detaching the VisualElement from the panel 
    /// pauses the scheduler, and reattaching it resumes the scheduler.
    /// </remarks>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// // This example uses the scheduler to animate the title logo by changing its background image 
    /// // to the next frame every 200 milliseconds.
    /// int m_CurrentTitleLogoFrame = 0;
    /// public List<Texture2D> m_TitleLogoFrames = new List<Texture2D>();
    /// // Animate title logo.
    /// var titleLogo = root.Q("menu-title-image");
    /// titleLogo?.schedule.Execute(() =>
    /// {
    ///     if (m_TitleLogoFrames.Count == 0)
    ///         return;
    ///
    ///         m_CurrentTitleLogoFrame = (m_CurrentTitleLogoFrame + 1) % m_TitleLogoFrames.Count;
    ///         var frame = m_TitleLogoFrames[m_CurrentTitleLogoFrame];
    ///         titleLogo.style.backgroundImage = frame;
    /// }).Every(200);
    /// ]]>
    /// </code>
    /// </example>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// // This example uses the scheduler to animate the scaling of a VisualElement.
    /// IVisualElementScheduledItem m_AnimationScheduler = null;
    /// 
    /// public void DoScale()
    /// {
    /// // Scale the VisualElement.
    /// }  
    /// 
    /// m_AnimationScheduler = this.schedule.Execute(DoScale).Every(1).StartingIn(0);
    /// 
    /// // Stop the animation
    /// m_AnimationScheduler.Pause();
    /// ]]>
    /// </code>
    /// </example>
    /// <remarks>
    /// SA: [[VisualElement.schedule]], [[VisualElement.panel]], [[IVisualElementScheduledItem]]
    /// </remarks>
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

    public partial class VisualElement : IVisualElementScheduler
    {
        /// <summary>
        /// Retrieves the [[IVisualElementScheduler]] associated to this VisualElement. Use it to enqueue actions.
        /// </summary>
        public IVisualElementScheduler schedule { get { return this; } }

        /// <summary>
        /// This class is needed in order for its VisualElement to be notified when the scheduler removes an item
        /// </summary>
        private abstract class BaseVisualElementScheduledItem : ScheduledItem, IVisualElementScheduledItem
        {
            public VisualElement element { get; private set; }
            public bool isScheduled = false;

            public bool isActive { get; private set; }
            public bool isDetaching { get; private set; }

            private readonly EventCallback<AttachToPanelEvent> m_OnAttachToPanelCallback;
            private readonly EventCallback<DetachFromPanelEvent> m_OnDetachFromPanelCallback;

            protected BaseVisualElementScheduledItem(VisualElement handler)
            {
                element = handler;
                m_OnAttachToPanelCallback = OnElementAttachToPanelCallback;
                m_OnDetachFromPanelCallback = OnElementDetachFromPanelCallback;
            }

            private void SetActive(bool action)
            {
                if (isActive != action)
                {
                    isActive = action;
                    if (isActive)
                    {
                        element.RegisterCallback(m_OnAttachToPanelCallback);
                        element.RegisterCallback(m_OnDetachFromPanelCallback);
                        SendActivation();
                    }
                    else
                    {
                        element.UnregisterCallback(m_OnAttachToPanelCallback);
                        element.UnregisterCallback(m_OnDetachFromPanelCallback);
                        SendDeactivation();
                    }
                }
            }

            private void SendActivation()
            {
                if (CanBeActivated())
                {
                    OnPanelActivate();
                }
            }

            private void SendDeactivation()
            {
                if (CanBeActivated())
                {
                    OnPanelDeactivate();
                }
            }

            private void OnElementAttachToPanelCallback(AttachToPanelEvent evt)
            {
                if (isActive)
                {
                    SendActivation();
                }
            }

            private void OnElementDetachFromPanelCallback(DetachFromPanelEvent evt)
            {
                if (!isActive)
                    return;

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
                if (!isDetaching)
                {
                    SetActive(false);
                }
            }

            public void Resume()
            {
                SetActive(true);
            }

            public void Pause()
            {
                SetActive(false);
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
