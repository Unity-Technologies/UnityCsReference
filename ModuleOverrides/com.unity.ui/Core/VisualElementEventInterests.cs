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
        private static uint s_NextParentVersion;

        // The version with which this element's m_CachedNextParentWithEventCallback was computed.
        private uint m_NextParentCachedVersion;

        // The version that children need to have to use this element as their m_CachedNextParentWithEventCallback.
        // This should be 0 if this element has no event callback, and non-0 if it has any.
        private uint m_NextParentRequiredVersion;

        // The last computed nextParentWithEventCallback for this element.
        // We make sure to reset this to null when an element's panel changes, to allow the GC to do its work.
        // This is used in performance-critical paths so we should avoid WeakReference or GCHandle (10x slower).
        private VisualElement m_CachedNextParentWithEventCallback;

        // Call this to force any children of this element to invalidate their m_CachedNextParentWithEventCallback.
        // Instead of actually overwriting the children's data, which might be a costly task, we instead mark the
        // old shared parent as having a newer version than what the children had seen, thus forcing them to lazily
        // reevaluate their next parent when queried about it.
        private void DirtyNextParentWithEventCallback()
        {
            if (m_CachedNextParentWithEventCallback != null &&
                m_NextParentCachedVersion == m_CachedNextParentWithEventCallback.m_NextParentRequiredVersion)
            {
                m_CachedNextParentWithEventCallback.m_NextParentRequiredVersion = ++s_NextParentVersion;
            }
        }

        // Makes this element appear as its children's next parent with an event callback, provided these children had
        // the same next parent as we did, opening a new required version for this element.
        // By invalidating our own nextParent's version, we also invalidate that of our children, by construction.
        private void SetAsNextParentWithEventCallback()
        {
            // If I'm already a reference point for my children, then my children have nothing to retarget.
            if (m_NextParentRequiredVersion != 0u)
                return;

            m_NextParentRequiredVersion = ++s_NextParentVersion;

            // All those pointing to my old parent might now point to me, so we make their version outdated
            if (m_CachedNextParentWithEventCallback != null &&
                m_NextParentCachedVersion == m_CachedNextParentWithEventCallback.m_NextParentRequiredVersion)
            {
                m_CachedNextParentWithEventCallback.m_NextParentRequiredVersion = ++s_NextParentVersion;
            }
        }

        // Returns the cached next parent if its cached version is up to date.
        internal bool GetCachedNextParentWithEventCallback(out VisualElement nextParent)
        {
            nextParent = m_CachedNextParentWithEventCallback;
            return nextParent != null && nextParent.m_NextParentRequiredVersion == m_NextParentCachedVersion;
        }

        // Returns or computes the exact next parent that has an event callback or is a composite root.
        // This is useful for quickly building PropagationPaths and parentEventCallbackCategories.
        internal VisualElement nextParentWithEventCallback
        {
            get
            {
                // Value is up to date, return it. This should be the most frequent case.
                if (GetCachedNextParentWithEventCallback(out var nextParent))
                {
                    return nextParent;
                }

                // Search for the next parent by climbing up until we find a suitable candidate
                for (var candidate = hierarchy.parent; candidate != null; candidate = candidate.hierarchy.parent)
                {
                    // Candidate is a proper next parent
                    if (candidate.m_NextParentRequiredVersion != 0u)
                    {
                        PropagateCachedNextParentWithEventCallback(candidate, candidate);
                        return candidate;
                    }

                    // Candidate has a fast path to a suitable parent
                    if (candidate.GetCachedNextParentWithEventCallback(out var candidateNextParent))
                    {
                        PropagateCachedNextParentWithEventCallback(candidateNextParent, candidate);
                        return candidateNextParent;
                    }
                }

                // This is the top element, return null and clear the cached reference (to allow the GC to do its work)
                m_CachedNextParentWithEventCallback = null;
                return null;
            }
        }

        // Sets new next parent across the hierarchy between this and the new parent
        private void PropagateCachedNextParentWithEventCallback(VisualElement nextParent, VisualElement stopParent)
        {
            for (var ve = this; ve != stopParent; ve = ve.hierarchy.parent)
            {
                ve.m_CachedNextParentWithEventCallback = nextParent;
                ve.m_NextParentCachedVersion = nextParent.m_NextParentRequiredVersion;
            }
        }

        private int m_EventCallbackCategories = 0;

        // An aggregate of the EventCategory values of all the calls to RegisterCallback for this element.
        // This also encodes the isCompositeRoot property, to simplify the nextParentWithEventCallback computation.
        internal int eventCallbackCategories
        {
            get => m_EventCallbackCategories;
            set
            {
                if (m_EventCallbackCategories != value)
                {
                    int diff = m_EventCallbackCategories ^ value;
                    if ((diff & (int)~EventCategoryFlags.TargetOnly) != 0)
                    {
                        SetAsNextParentWithEventCallback();
                        IncrementVersion(VersionChangeType.EventCallbackCategories);
                    }
                    else
                    {
                        // Don't invalidate children's categories, but do maintain parentCategories >= targetCategories
                        m_CachedEventCallbackParentCategories |= value;
                    }
                    m_EventCallbackCategories = value;
                }
            }
        }

        private int m_CachedEventCallbackParentCategories = 0;

        // Returns or computes the combined eventCallbackCategories of this element and all its parents.
        // The cached version of this property will be invalidated frequently, so this needs to be relatively cheap.
        internal int eventCallbackParentCategories
        {
            get
            {
                if (elementPanel == null)
                    return -1;

                if (isEventCallbackParentCategoriesDirty)
                {
                    UpdateCallbackParentCategories();
                    isEventCallbackParentCategoriesDirty = false;
                }

                return m_CachedEventCallbackParentCategories;
            }
        }

        internal bool isEventCallbackParentCategoriesDirty
        {
            get => (m_Flags & VisualElementFlags.EventCallbackParentCategoriesDirty) == VisualElementFlags.EventCallbackParentCategoriesDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.EventCallbackParentCategoriesDirty : m_Flags & ~VisualElementFlags.EventCallbackParentCategoriesDirty;
        }

        private void UpdateCallbackParentCategories()
        {
            m_CachedEventCallbackParentCategories = m_EventCallbackCategories;

            // Composite roots receive DefaultAction during event propagation as though they were the leaf target.
            if (isCompositeRoot)
                m_CachedEventCallbackParentCategories |= m_DefaultActionEventCategories;

            var nextParent = nextParentWithEventCallback;
            if (nextParent == null)
                return;

            // Recursively compute categories for next parent with event callbacks
            m_CachedEventCallbackParentCategories |= nextParent.eventCallbackParentCategories;

            // Fill in the gap between this and the next parent with non-identical callback info.
            if (hierarchy.parent != null)
            {
                for (var ve = hierarchy.parent; ve != nextParent; ve = ve.hierarchy.parent)
                {
                    ve.m_CachedEventCallbackParentCategories = m_CachedEventCallbackParentCategories;
                    ve.isEventCallbackParentCategoriesDirty = false;
                }
            }
        }

        // Returns true if this element might have a RegisterCallback on an event of the given category.
        internal bool HasEventCallbacks(EventCategory eventCategory) =>
            0 != (eventCallbackCategories & (1 << (int)eventCategory));

        // Returns true if this element or any of its parents might have a RegisterCallback for the given category.
        internal bool HasParentEventCallbacks(EventCategory eventCategory) =>
            0 != (eventCallbackParentCategories & (1 << (int)eventCategory));

        // Virtual methods that only react to specific event categories can use the [EventInterest] attribute to allow
        // UI Toolkit to skip any unrelated events. EventInterests from base classes are automatically carried over.
        // Applies to the "ExecuteDefaultAction" and "ExecuteDefaultActionAtTarget" methods.
        // Since this is all type-specific data, it could eventually be stored in a shared object.
        private readonly int m_DefaultActionEventCategories;
        private readonly int m_DefaultActionAtTargetEventCategories;

        // Use this to fully skip an event that bubbles or trickles down
        internal bool HasParentEventCallbacksOrDefaultActions(EventCategory eventCategory) =>
            0 != ((m_DefaultActionEventCategories | m_DefaultActionAtTargetEventCategories |
                eventCallbackParentCategories) & (1 << (int)eventCategory));

        // Use this to fully skip an event that affects only its target
        internal bool HasEventCallbacksOrDefaultActions(EventCategory eventCategory) =>
            0 != ((m_DefaultActionEventCategories | m_DefaultActionAtTargetEventCategories |
                eventCallbackCategories) & (1 << (int)eventCategory));

        // Use this to skip event propagation for an event that bubbles or trickles down.
        // Do not use this to skip the ExecuteDefaultAction last phase, however.
        internal bool HasParentEventCallbacksOrDefaultActionAtTarget(EventCategory eventCategory) =>
            0 != ((m_DefaultActionAtTargetEventCategories | eventCallbackParentCategories) & (1 << (int)eventCategory));

        // Use this to skip event propagation for an event that affects only its target.
        // Do not use this to skip the ExecuteDefaultAction last phase, however.
        internal bool HasEventCallbacksOrDefaultActionAtTarget(EventCategory eventCategory) =>
            0 != ((m_DefaultActionAtTargetEventCategories | eventCallbackCategories) & (1 << (int)eventCategory));

        // Use this to skip ExecuteDefaultAction.
        internal bool HasDefaultAction(EventCategory eventCategory) =>
            0 != (m_DefaultActionEventCategories & (1 << (int)eventCategory));
    }

    internal static class EventInterestReflectionUtils
    {
        // The type-specific fully-combined event interests for the 3 admissible virtual method families.
        private struct DefaultEventInterests
        {
            public int DefaultActionCategories;
            public int DefaultActionAtTargetCategories;
        }

        private static readonly Dictionary<Type, DefaultEventInterests> s_DefaultEventInterests =
            new Dictionary<Type, DefaultEventInterests>();

        // Initialize this VisualElement's default categories according to its fully-resolved Type.
        internal static void GetDefaultEventInterests(Type elementType, out int defaultActionCategories,
            out int defaultActionAtTargetCategories)
        {
            if (!s_DefaultEventInterests.TryGetValue(elementType, out var categories))
            {
                var ancestorType = elementType.BaseType;
                if (ancestorType != null)
                {
                    GetDefaultEventInterests(ancestorType, out categories.DefaultActionCategories,
                        out categories.DefaultActionAtTargetCategories);
                }

                categories.DefaultActionCategories |=
                    ComputeDefaultEventInterests(elementType, CallbackEventHandler.ExecuteDefaultActionName) |
                    ComputeDefaultEventInterests(elementType, nameof(CallbackEventHandler.ExecuteDefaultActionDisabled));

                categories.DefaultActionAtTargetCategories |=
                    ComputeDefaultEventInterests(elementType, CallbackEventHandler.ExecuteDefaultActionAtTargetName) |
                    ComputeDefaultEventInterests(elementType, nameof(CallbackEventHandler.ExecuteDefaultActionDisabledAtTarget));

                s_DefaultEventInterests.Add(elementType, categories);
            }

            defaultActionCategories = categories.DefaultActionCategories;
            defaultActionAtTargetCategories = categories.DefaultActionAtTargetCategories;
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
        IMGUI,
        Reserved = 31   // used by Panel.m_RootContainer to force it to be a nextParentWithEventCallback
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
            1 << EventCategory.EnterLeaveWindow |
            1 << EventCategory.Keyboard |
            1 << EventCategory.Command |
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
        /// For example, a class that overrides the <see cref="CallbackEventHandler.ExecuteDefaultAction"/> method and
        /// checks if an enabled flag is active before calling its base.ExecuteDefaultAction method would use this option.
        /// </summary>
        Inherit = EventCategoryFlags.None,

        /// <summary>
        /// Use the <see cref="EventInterestOptions.AllEventTypes"/> option when the method with an
        /// <see cref="EventInterestAttribute"/> doesn't have a specific filter for the event types it uses, or wants
        /// to receive all possible event types.
        /// For example, a class that overrides <see cref="CallbackEventHandler.ExecuteDefaultAction"/> and logs a
        /// message every time an event of any kind is received would require this option.
        /// </summary>
        AllEventTypes = EventCategoryFlags.All,
    }

    internal enum EventInterestOptionsInternal
    {
        TriggeredByOS = EventCategoryFlags.TriggeredByOS
    }

    /// <summary>
    /// Optional attribute on overrides of <see cref="CallbackEventHandler.ExecuteDefaultAction"/> or
    /// <see cref="CallbackEventHandler.ExecuteDefaultActionAtTarget"/>
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
