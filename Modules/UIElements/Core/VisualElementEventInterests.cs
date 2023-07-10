// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.UIElements
{
    partial class VisualElement
    {
        // Virtual methods that only react to specific event categories can use the [EventInterest] attribute to allow
        // UI Toolkit to skip any unrelated events. EventInterests from base classes are automatically carried over.
        // Applies to the "HandleEventTrickleDown" and "HandleEventBubbleUp" methods.

        // Since this is type-specific data, it could eventually be stored in a shared object.
        private readonly int m_TrickleDownHandleEventCategories;
        private readonly int m_BubbleUpHandleEventCategories;

        private int m_BubbleUpEventCallbackCategories = 0;
        private int m_TrickleDownEventCallbackCategories = 0;
        private int m_EventInterestSelfCategories = 0;
        private int m_CachedEventInterestParentCategories = 0;

        private static uint s_NextParentVersion;

        // The version with which this element's m_CachedNextParentWithEventInterests was computed.
        private uint m_NextParentCachedVersion;

        // The version that children need to have to use this element as their m_CachedNextParentWithEventInterests.
        // This should be 0 if this element has no event callback, and non-0 if it has any.
        private uint m_NextParentRequiredVersion;

        // The last computed nextParentWithEventInterests for this element.
        // We make sure to reset this to null when an element's panel changes, to allow the GC to do its work.
        // This is used in performance-critical paths so we should avoid WeakReference or GCHandle (10x slower).
        private VisualElement m_CachedNextParentWithEventInterests;

        // Call this to force any children of this element to invalidate their m_CachedNextParentWithEventInterests.
        // Instead of actually overwriting the children's data, which might be a costly task, we instead mark the
        // old shared parent as having a newer version than what the children had seen, thus forcing them to lazily
        // reevaluate their next parent when queried about it.
        private void DirtyNextParentWithEventInterests()
        {
            if (m_CachedNextParentWithEventInterests != null &&
                m_NextParentCachedVersion == m_CachedNextParentWithEventInterests.m_NextParentRequiredVersion)
            {
                m_CachedNextParentWithEventInterests.m_NextParentRequiredVersion = ++s_NextParentVersion;
            }
        }

        // Makes this element appear as its children's next parent with an event callback, provided these children had
        // the same next parent as we did, opening a new required version for this element.
        // By invalidating our own nextParent's version, we also invalidate that of our children, by construction.
        internal void SetAsNextParentWithEventInterests()
        {
            // If I'm already a reference point for my children, then my children have nothing to retarget.
            if (m_NextParentRequiredVersion != 0u)
                return;

            m_NextParentRequiredVersion = ++s_NextParentVersion;

            // All those pointing to my old parent might now point to me, so we make their version outdated
            if (m_CachedNextParentWithEventInterests != null &&
                m_NextParentCachedVersion == m_CachedNextParentWithEventInterests.m_NextParentRequiredVersion)
            {
                m_CachedNextParentWithEventInterests.m_NextParentRequiredVersion = ++s_NextParentVersion;
            }
        }

        // Returns the cached next parent if its cached version is up to date.
        internal bool GetCachedNextParentWithEventInterests(out VisualElement nextParent)
        {
            nextParent = m_CachedNextParentWithEventInterests;
            return nextParent != null && nextParent.m_NextParentRequiredVersion == m_NextParentCachedVersion;
        }

        // Returns or computes the exact next parent that has an event callback or HandleEvent action.
        // This is useful for quickly building PropagationPaths and parentEventCallbackCategories.
        // Panel root must always be the last parent of the chain if an element in inside a panel.
        internal VisualElement nextParentWithEventInterests
        {
            get
            {
                // Value is up to date, return it. This should be the most frequent case.
                if (GetCachedNextParentWithEventInterests(out var nextParent))
                {
                    return nextParent;
                }

                // Search for the next parent by climbing up until we find a suitable candidate
                for (var candidate = hierarchy.parent; candidate != null; candidate = candidate.hierarchy.parent)
                {
                    // Candidate is a proper next parent
                    if (candidate.m_NextParentRequiredVersion != 0u)
                    {
                        PropagateCachedNextParentWithEventInterests(candidate, candidate);
                        return candidate;
                    }

                    // Candidate has a fast path to a suitable parent
                    if (candidate.GetCachedNextParentWithEventInterests(out var candidateNextParent))
                    {
                        PropagateCachedNextParentWithEventInterests(candidateNextParent, candidate);
                        return candidateNextParent;
                    }
                }

                // This is the top element, return null and clear the cached reference (to allow the GC to do its work)
                m_CachedNextParentWithEventInterests = null;
                return null;
            }
        }

        // Sets new next parent across the hierarchy between this and the new parent
        private void PropagateCachedNextParentWithEventInterests(VisualElement nextParent, VisualElement stopParent)
        {
            for (var ve = this; ve != stopParent; ve = ve.hierarchy.parent)
            {
                ve.m_CachedNextParentWithEventInterests = nextParent;
                ve.m_NextParentCachedVersion = nextParent.m_NextParentRequiredVersion;
            }
        }

        internal void AddEventCallbackCategories(int eventCategories, TrickleDown trickleDown)
        {
            if (trickleDown == TrickleDown.TrickleDown)
                m_TrickleDownEventCallbackCategories |= eventCategories;
            else
                m_BubbleUpEventCallbackCategories |= eventCategories;
            UpdateEventInterestSelfCategories();
        }

        // An aggregate of the EventCategory values of all the calls to RegisterCallback for this element
        // and overrides of HandleEventTrickleDown or HandleEventBubbleUp across this element's class hierarchy.
        internal int eventInterestSelfCategories => m_EventInterestSelfCategories;

        // Returns or computes the combined EventCategory interests of this element and all its parents.
        // The cached version of this property will be invalidated frequently, so this needs to be relatively cheap.
        internal int eventInterestParentCategories
        {
            get
            {
                if (elementPanel == null)
                    return -1;

                if (isEventInterestParentCategoriesDirty)
                {
                    UpdateEventInterestParentCategories();
                    isEventInterestParentCategoriesDirty = false;
                }

                return m_CachedEventInterestParentCategories;
            }
        }

        internal bool isEventInterestParentCategoriesDirty
        {
            get => (m_Flags & VisualElementFlags.EventInterestParentCategoriesDirty) == VisualElementFlags.EventInterestParentCategoriesDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.EventInterestParentCategoriesDirty : m_Flags & ~VisualElementFlags.EventInterestParentCategoriesDirty;
        }

        private void UpdateEventInterestSelfCategories()
        {
            int value = m_TrickleDownHandleEventCategories | m_BubbleUpHandleEventCategories |
                        m_TrickleDownEventCallbackCategories | m_BubbleUpEventCallbackCategories;

            if (m_EventInterestSelfCategories != value)
            {
                int diff = m_EventInterestSelfCategories ^ value;
                if ((diff & (int)~EventCategoryFlags.TargetOnly) != 0)
                {
                    SetAsNextParentWithEventInterests();
                    IncrementVersion(VersionChangeType.EventCallbackCategories);
                }
                else
                {
                    // Don't invalidate children's categories, but do maintain parentCategories >= targetCategories
                    m_CachedEventInterestParentCategories |= value;
                }

                m_EventInterestSelfCategories = value;
            }
        }

        private void UpdateEventInterestParentCategories()
        {
            m_CachedEventInterestParentCategories = m_EventInterestSelfCategories;

            var nextParent = nextParentWithEventInterests;
            if (nextParent == null)
                return;

            // Recursively compute categories for next parent with event callbacks
            m_CachedEventInterestParentCategories |= nextParent.eventInterestParentCategories;

            // Fill in the gap between this and the next parent with non-identical callback info.
            if (hierarchy.parent != null)
            {
                for (var ve = hierarchy.parent; ve != nextParent; ve = ve.hierarchy.parent)
                {
                    ve.m_CachedEventInterestParentCategories = m_CachedEventInterestParentCategories;
                    ve.isEventInterestParentCategoriesDirty = false;
                }
            }
        }

        // Returns true if this element or any of its parents might have a RegisterCallback or a
        // HandleEventTrickleDown or HandleEventBubbleUp override for the given category.
        // Use this to skip an event that bubbles up or trickles down but with no interest from its target's hierarchy.
        internal bool HasParentEventInterests(EventCategory eventCategory) =>
            0 != (eventInterestParentCategories & (1 << (int)eventCategory));
        internal bool HasParentEventInterests(int eventCategories) =>
            0 != (eventInterestParentCategories & eventCategories);

        // Returns true if this element itself has a RegisterCallback or a HandleEventTrickleDown/BubbleUp override.
        // Use this to skip an event that affects only its target, if the target has no interest for it.
        internal bool HasSelfEventInterests(EventCategory eventCategory) =>
            0 != (m_EventInterestSelfCategories & (1 << (int)eventCategory));
        internal bool HasSelfEventInterests(int eventCategories) =>
            0 != (m_EventInterestSelfCategories & eventCategories);
        internal bool HasTrickleDownEventInterests(int eventCategories) =>
            0 != ((m_TrickleDownHandleEventCategories | m_TrickleDownEventCallbackCategories) & eventCategories);
        internal bool HasBubbleUpEventInterests(int eventCategories) =>
            0 != ((m_BubbleUpHandleEventCategories | m_BubbleUpEventCallbackCategories) & eventCategories);

        // Returns true if this element might have TrickleDown or BubbleUp callbacks on an event of the given category.
        // The EventDispatcher uses this to skip InvokeCallbacks.
        internal bool HasTrickleDownEventCallbacks(int eventCategories) =>
            0 != (m_TrickleDownEventCallbackCategories & eventCategories);
        internal bool HasBubbleUpEventCallbacks(int eventCategories) =>
            0 != (m_BubbleUpEventCallbackCategories & eventCategories);

        // Returns true if this element has HandleEventTrickleDown or HandleEventBubbleUp overrides.
        // The EventDispatcher uses this to skip HandleEventTrickleDown and HandleEventBubbleUp.
        internal bool HasTrickleDownHandleEvent(EventCategory eventCategory) =>
            0 != (m_TrickleDownHandleEventCategories & (1 << (int)eventCategory));
        internal bool HasTrickleDownHandleEvent(int eventCategories) =>
            0 != (m_TrickleDownHandleEventCategories & eventCategories);
        internal bool HasBubbleUpHandleEvent(EventCategory eventCategory) =>
            0 != (m_BubbleUpHandleEventCategories & (1 << (int)eventCategory));
        internal bool HasBubbleUpHandleEvent(int eventCategories) =>
            0 != (m_BubbleUpHandleEventCategories & eventCategories);
    }

    internal static class EventInterestReflectionUtils
    {
        // The type-specific fully-combined event interests for the 3 admissible virtual method families.
        private struct DefaultEventInterests
        {
            public int DefaultActionCategories;
            public int DefaultActionAtTargetCategories;
            public int HandleEventTrickleDownCategories;
            public int HandleEventBubbleUpCategories;
        }

        private static readonly Dictionary<Type, DefaultEventInterests> s_DefaultEventInterests =
            new Dictionary<Type, DefaultEventInterests>();

        // Initialize this VisualElement's default categories according to its fully-resolved Type.
        internal static void GetDefaultEventInterests(Type elementType,
            out int defaultActionCategories, out int defaultActionAtTargetCategories,
            out int handleEventTrickleDownCategories, out int handleEventBubbleUpCategories)
        {
            if (!s_DefaultEventInterests.TryGetValue(elementType, out var categories))
            {
                var ancestorType = elementType.BaseType;
                if (ancestorType != null)
                {
                    GetDefaultEventInterests(ancestorType,
                        out categories.DefaultActionCategories, out categories.DefaultActionAtTargetCategories,
                        out categories.HandleEventTrickleDownCategories, out categories.HandleEventBubbleUpCategories);
                }

                categories.DefaultActionCategories |=
                    ComputeDefaultEventInterests(elementType, CallbackEventHandler.ExecuteDefaultActionName) |
// Disable deprecation warnings so we can access legacy method ExecuteDefaultActionDisabled
#pragma warning disable 618
                    ComputeDefaultEventInterests(elementType, nameof(CallbackEventHandler.ExecuteDefaultActionDisabled));
#pragma warning restore 618

                categories.DefaultActionAtTargetCategories |=
                    ComputeDefaultEventInterests(elementType, CallbackEventHandler.ExecuteDefaultActionAtTargetName) |
// Disable deprecation warnings so we can access legacy method ExecuteDefaultActionDisabledAtTarget
#pragma warning disable 618
                    ComputeDefaultEventInterests(elementType, nameof(CallbackEventHandler.ExecuteDefaultActionDisabledAtTarget));
#pragma warning restore 618

                categories.HandleEventTrickleDownCategories |=
                    ComputeDefaultEventInterests(elementType, CallbackEventHandler.HandleEventTrickleDownName) |
                    ComputeDefaultEventInterests(elementType, nameof(CallbackEventHandler.HandleEventTrickleDownDisabled));

                categories.HandleEventBubbleUpCategories |=
                    ComputeDefaultEventInterests(elementType, CallbackEventHandler.HandleEventBubbleUpName) |
                    ComputeDefaultEventInterests(elementType, nameof(CallbackEventHandler.HandleEventBubbleUpDisabled));

                s_DefaultEventInterests.Add(elementType, categories);
            }

            defaultActionCategories = categories.DefaultActionCategories;
            defaultActionAtTargetCategories = categories.DefaultActionAtTargetCategories;
            handleEventTrickleDownCategories = categories.HandleEventTrickleDownCategories;
            handleEventBubbleUpCategories = categories.HandleEventBubbleUpCategories;
        }

        // Compute one level of EventInterests, for the given type. Those values can be combined with that of the
        // derived types chain to compute the combined event interests for a given VisualElement type.
        private static int ComputeDefaultEventInterests(Type elementType, string methodName)
        {
            const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic;

            var methodInfo = elementType.GetMethod(methodName, flags);
            if (methodInfo == null)
                return 0;

            bool found = false;
            int categories = 0;
            var attributes = methodInfo.GetCustomAttributes(typeof(EventInterestAttribute), false);
            foreach (EventInterestAttribute attribute in attributes)
            {
                found = true;
                if (attribute.eventTypes != null)
                {
                    foreach (var eventType in attribute.eventTypes)
                        categories |= 1 << (int)GetEventCategory(eventType);
                }
                categories |= (int)attribute.categoryFlags;
            }

            return found ? categories : -1;
        }

        private static readonly Dictionary<Type, EventCategory> s_EventCategories =
            new Dictionary<Type, EventCategory>();

        internal static EventCategory GetEventCategory(Type eventType)
        {
            if (s_EventCategories.TryGetValue(eventType, out var category))
                return category;

            var attributes = eventType.GetCustomAttributes(typeof(EventCategoryAttribute), true);
            foreach (EventCategoryAttribute attribute in attributes)
            {
                category = attribute.category;
                s_EventCategories.Add(eventType, category);
                return category;
            }

            throw new ArgumentOutOfRangeException(nameof(eventType), "Type must derive from EventBase<T>");
        }
    }

    // This type is kept internal for now, but should be made public eventually, to give users access to a bit of
    // performance optimisation for their custom event types.
    /// <summary>
    /// Represents logical groups of events that are usually handled together by controls.
    /// </summary>
    /// <remarks>
    /// The <see cref="EventDispatcher"/> uses this to determine if an event needs to be fully dispatched by checking
    /// if its propagation path contains any callbacks to the family of events being dispatched.
    /// If it's determined there's no effect from the dispatching of the event, the event is likely skipped entirely.
    /// </remarks>
    /// <seealso cref="EventInterestAttribute"/>
    internal enum EventCategory
    {
        Default = 0,
        Pointer,
        PointerMove,
        PointerDown,
        EnterLeave,
        EnterLeaveWindow,
        Keyboard,
        Geometry,
        Style,
        ChangeValue,
        Bind,
        Focus,
        ChangePanel,
        StyleTransition,
        Navigation,
        Command,
        Tooltip,
        DragAndDrop,
        IMGUI
    }

    // Event category aggregator and useful shorthands for internal use.
    [Flags]
    internal enum EventCategoryFlags
    {
        None = 0,
        All = -1,

        // All events that have an equivalent IMGUI event
        TriggeredByOS = 1 << EventCategory.Pointer |
            1 << EventCategory.PointerMove |
            1 << EventCategory.PointerDown |
            1 << EventCategory.EnterLeaveWindow |
            1 << EventCategory.Keyboard |
            1 << EventCategory.Command |
            1 << EventCategory.DragAndDrop |
            1 << EventCategory.IMGUI,

        // Events types that won't trigger parent callbacks
        TargetOnly = 1 << EventCategory.EnterLeaveWindow |
            1 << EventCategory.Geometry |
            1 << EventCategory.Style |
            1 << EventCategory.Bind |
            1 << EventCategory.ChangePanel
    }

    /// <summary>
    /// Options used as arguments for EventInterestAttribute when the affected method treats events in a general,
    /// non type-specific manner.
    /// </summary>
    public enum EventInterestOptions
    {
        /// <summary>
        /// Use the <see cref="Inherit"/> option when only the events needed by the base
        /// class are required.
        /// For example, a class that overrides the <see cref="CallbackEventHandler.HandleEventBubbleUp"/>
        /// method and checks if an enabled flag is active before calling its base.ExecuteDefaultActionAtTarget method
        /// would use this option.
        /// </summary>
        Inherit = EventCategoryFlags.None,

        /// <summary>
        /// Use the <see cref="EventInterestOptions.AllEventTypes"/> option when the method with an
        /// <see cref="EventInterestAttribute"/> doesn't have a specific filter for the event types it uses, or wants
        /// to receive all possible event types.
        /// For example, a class that overrides <see cref="CallbackEventHandler.HandleEventBubbleUp"/> and logs
        /// a message every time an event of any kind is received would require this option.
        /// </summary>
        AllEventTypes = EventCategoryFlags.All,
    }

    internal enum EventInterestOptionsInternal
    {
        TriggeredByOS = EventCategoryFlags.TriggeredByOS
    }

    /// <summary>
    /// Optional attribute on overrides of <see cref="CallbackEventHandler.HandleEventBubbleUp"/>.
    /// </summary>
    /// <remarks>
    /// Use this to specify all the event types that these methods require to operate.
    /// If an override of one of these methods doesn't have any <see cref="EventInterestAttribute"/>, UI Toolkit will
    /// assume that the method doesn't have enough information on what event types it needs, and will conservatively
    /// send all incoming events to that method.
    /// It is generally a good idea to use the <see cref="EventInterestAttribute"/> attribute because it allows
    /// UI Toolkit to perform optimizations and not calculate all the information related to an event if it isn't used
    /// by the method that would receive it.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class EventInterestAttribute : Attribute
    {
        internal Type[] eventTypes;
        internal EventCategoryFlags categoryFlags = EventCategoryFlags.None;

        /// <summary>
        /// Use this constructor when the affected method uses only specific event types that can easily be determined
        /// at compile time.
        /// Multiple <see cref="EventInterestAttribute"/> can be used on the same method to add more type interests.
        /// </summary>
        /// <param name="eventTypes">
        /// The affected method is guaranteed to be invoked if the incoming event has any of the specified types
        /// in this argument.
        /// </param>
        public EventInterestAttribute(params Type[] eventTypes)
        {
            this.eventTypes = eventTypes;
        }

        /// <summary>
        /// Use this constructor when the affected method treats events in a general, non type-specific manner.
        /// See <see cref="EventInterestOptions"/> for more information on the available argument values.
        /// </summary>
        /// <param name="interests">
        /// The affected method is guaranteed to be invoked if the incoming event has any of the specified types
        /// in this argument.
        /// </param>
        public EventInterestAttribute(EventInterestOptions interests)
        {
            categoryFlags = (EventCategoryFlags)interests;
        }

        internal EventInterestAttribute(EventInterestOptionsInternal interests)
        {
            categoryFlags = (EventCategoryFlags)interests;
        }
    }

    /// <summary>
    /// Use on any derived class of <see cref="EventBase"/> to specify what <see cref="EventCategory"/> this event
    /// belongs to.
    /// Each event belongs to exactly one category. If no attribute is used, the event will have the same category as
    /// its base class, or the <see cref="EventCategory.Default"/> category if no attribute exists on any base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class EventCategoryAttribute : Attribute
    {
        internal EventCategory category;
        /// <summary>
        /// Initializes and returns an instance of EventCategoryAttribute
        /// </summary>
        /// <param name="category">The <see cref="EventCategory"/> that this event belongs to.</param>
        public EventCategoryAttribute(EventCategory category)
        {
            this.category = category;
        }
    }
}
