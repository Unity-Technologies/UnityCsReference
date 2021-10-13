// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for all transition events.
    /// </summary>
    public interface ITransitionEvent
    {
        /// <summary>
        /// The names of the properties associated with the transition.
        /// </summary>
        StylePropertyNameCollection stylePropertyNames { get; }
        /// <summary>
        /// The number of seconds the transition has been running, excluding delay phase time.
        /// </summary>
        double elapsedTime { get; }
    }

    /// <summary>
    /// Collection of <see cref="StylePropertyName"/>.
    /// </summary>
    public struct StylePropertyNameCollection : IEnumerable<StylePropertyName>
    {
        /// <summary>
        /// Enumerates the elements of a <see cref="StylePropertyNameCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<StylePropertyName>
        {
            List<StylePropertyName>.Enumerator m_Enumerator;
            internal Enumerator(List<StylePropertyName>.Enumerator enumerator)
            {
                m_Enumerator = enumerator;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="StylePropertyNameCollection"/>.
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            /// <returns>The element in the <see cref="StylePropertyNameCollection"/> at the current position of the enumerator.</returns>
            public StylePropertyName Current => m_Enumerator.Current;

            object System.Collections.IEnumerator.Current => Current;

            public void Reset() {}

            /// <summary>
            /// Releases all resources used by the <see cref="StylePropertyNameCollection"/> enumerator.
            /// </summary>
            public void Dispose() => m_Enumerator.Dispose();
        }

        internal List<StylePropertyName> propertiesList;

        internal StylePropertyNameCollection(List<StylePropertyName> list)
        {
            propertiesList = list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="StylePropertyNameCollection"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="StylePropertyNameCollection"/>.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(propertiesList.GetEnumerator());
        }

        IEnumerator<StylePropertyName> IEnumerable<StylePropertyName>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///  Determines whether a <see cref="StylePropertyNameCollection"/> contains the specified element.
        /// </summary>
        /// <param name="stylePropertyName">The element to locate in the <see cref="StylePropertyNameCollection"/>.</param>
        /// <returns>true if the <see cref="StylePropertyNameCollection"/> contains the specified element; otherwise, false.</returns>
        public bool Contains(StylePropertyName stylePropertyName)
        {
            using (var e = propertiesList.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (e.Current == stylePropertyName)
                        return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Transition events abstract base class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [EventCategory(EventCategory.StyleTransition)]
    public abstract class TransitionEventBase<T> : EventBase<T>, ITransitionEvent
        where T : TransitionEventBase<T>, new()
    {
        /// <summary>
        /// The names of the properties associated with the transition.
        /// </summary>
        public StylePropertyNameCollection stylePropertyNames { get; }

        /// <summary>
        /// The number of seconds the transition has been running, excluding delay phase time.
        /// </summary>
        public double elapsedTime { get; protected set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected TransitionEventBase()
        {
            stylePropertyNames = new StylePropertyNameCollection(new List<StylePropertyName>());
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
            stylePropertyNames.propertiesList.Clear();
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
            e.stylePropertyNames.propertiesList.Add(stylePropertyName);
            e.elapsedTime = elapsedTime;
            return e;
        }

        /// <summary>
        ///  Determines whether the <see cref="ITransitionEvent"/> affects the specified property.
        /// </summary>
        /// <param name="stylePropertyName">The property to check against.</param>
        /// <returns>true if the <see cref="ITransitionEvent"/> affects the specified property; otherwise, false.</returns>
        public bool AffectsProperty(StylePropertyName stylePropertyName) => stylePropertyNames.Contains(stylePropertyName);
    }

    /// <summary>
    /// Event sent when a transition is created (i.e. added to the set of running transitions).
    /// </summary>
    public sealed class TransitionRunEvent : TransitionEventBase<TransitionRunEvent>
    {
        static TransitionRunEvent()
        {
            SetCreateFunction(() => new TransitionRunEvent());
        }
    }

    /// <summary>
    /// Event sent when a transition's delay phase ends.
    /// </summary>
    public sealed class TransitionStartEvent : TransitionEventBase<TransitionStartEvent>
    {
        static TransitionStartEvent()
        {
            SetCreateFunction(() => new TransitionStartEvent());
        }
    }

    /// <summary>
    /// Event sent at the completion of the transition. In the case where a transition is removed before completion then the event will not fire.
    /// </summary>
    public sealed class TransitionEndEvent : TransitionEventBase<TransitionEndEvent>
    {
        static TransitionEndEvent()
        {
            SetCreateFunction(() => new TransitionEndEvent());
        }
    }

    /// <summary>
    /// Event sent when a transition is canceled.
    /// </summary>
    public sealed class TransitionCancelEvent : TransitionEventBase<TransitionCancelEvent>
    {
        static TransitionCancelEvent()
        {
            SetCreateFunction(() => new TransitionCancelEvent());
        }
    }
}
