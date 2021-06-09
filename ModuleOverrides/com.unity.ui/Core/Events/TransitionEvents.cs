// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for all transition events.
    /// </summary>
    public interface ITransitionEvent
    {
        /// <undoc/>
        StylePropertyName stylePropertyName { get; }
        /// <undoc/>
        double elapsedTime { get; }
    }

    /// <summary>
    /// Transition events abstract base class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TransitionEventBase<T> : EventBase<T>, ITransitionEvent
        where T : TransitionEventBase<T>, new()
    {
        /// <undoc/>
        public StylePropertyName stylePropertyName { get; protected set; }
        /// <undoc/>
        public double elapsedTime { get; protected set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected TransitionEventBase()
        {
            LocalInit();
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles;
            propagateToIMGUI = false;
            stylePropertyName = default;
            elapsedTime = default;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values.
        /// Use this function instead of creating new events.
        /// Events obtained from this method should be released back to the pool using Dispose().
        /// </summary>
        /// <param name="stylePropertyName">The name of the style property.</param>
        /// <param name="elapsedTime">The elapsed time.</param>
        /// <returns>An initialized transition event.</returns>
        public static T GetPooled(StylePropertyName stylePropertyName, double elapsedTime)
        {
            T e = GetPooled();
            e.stylePropertyName = stylePropertyName;
            e.elapsedTime = elapsedTime;
            return e;
        }
    }

    /// <summary>
    /// Event sent when a transition is created (i.e. added to the set of running transitions).
    /// </summary>
    public sealed class TransitionRunEvent : TransitionEventBase<TransitionRunEvent>
    {
    }

    /// <summary>
    /// Event sent when a transition's delay phase ends.
    /// </summary>
    public sealed class TransitionStartEvent : TransitionEventBase<TransitionStartEvent>
    {
    }

    /// <summary>
    /// Event sent at the completion of the transition. In the case where a transition is removed before completion then the event will not fire.
    /// </summary>
    public sealed class TransitionEndEvent : TransitionEventBase<TransitionEndEvent>
    {
    }

    /// <summary>
    /// Event sent when a transition is canceled.
    /// </summary>
    public sealed class TransitionCancelEvent : TransitionEventBase<TransitionCancelEvent>
    {
    }
}
