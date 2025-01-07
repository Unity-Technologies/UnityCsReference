// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for panel change events.
    /// </summary>
    /// <remarks>
    /// Refer to the [[wiki:UIE-Panel-Events|Panel events]] manual page for more information and examples.
    /// </remarks>
    public interface IPanelChangedEvent
    {
    }

    /// <summary>
    /// Abstract base class for events notifying of a panel change.
    /// </summary>
    /// <remarks>
    /// Refer to the [[wiki:UIE-Panel-Events|Panel events]] manual page for more information and examples.
    /// </remarks>
    [EventCategory(EventCategory.ChangePanel)]
    public abstract class PanelChangedEventBase<T> : EventBase<T>, IPanelChangedEvent where T : PanelChangedEventBase<T>, new()
    {
        /// <summary>
        /// In the case of AttachToPanelEvent, the panel to which the event target element was attached. In the case of DetachFromPanelEvent, the panel from which the event target element is detached.
        /// </summary>
        public IPanel originPanel { get; private set; }
        /// <summary>
        /// In the case of AttachToPanelEvent, the panel to which the event target element is now attached. In the case of DetachFromPanelEvent, the panel to which the event target element will be attached.
        /// </summary>
        public IPanel destinationPanel { get; private set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            originPanel = null;
            destinationPanel = null;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="originPanel">Sets the originPanel property of the event.</param>
        /// <param name="destinationPanel">Sets the destinationPanel property of the event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(IPanel originPanel, IPanel destinationPanel)
        {
            T e = GetPooled();
            e.originPanel = originPanel;
            e.destinationPanel = destinationPanel;
            return e;
        }

        protected PanelChangedEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent after an element is added to an element that is a descendant of a panel.
    /// </summary>
    /// <remarks>
    /// Don't modify the parent of the element in your registered callbacks.
    /// </remarks>
    public class AttachToPanelEvent : PanelChangedEventBase<AttachToPanelEvent>
    {
        static AttachToPanelEvent()
        {
            SetCreateFunction(() => new AttachToPanelEvent());
        }
    }

    /// <summary>
    /// Event sent just before an element is detached from its parent, if the parent is the descendant of a panel.
    /// </summary>
    /// <remarks>
    /// Don't modify the parent of the element in your registered callbacks.
    /// </remarks>
    public class DetachFromPanelEvent : PanelChangedEventBase<DetachFromPanelEvent>
    {
        static DetachFromPanelEvent()
        {
            SetCreateFunction(() => new DetachFromPanelEvent());
        }
    }
}
