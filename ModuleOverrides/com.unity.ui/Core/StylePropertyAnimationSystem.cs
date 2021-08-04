// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal interface IStylePropertyAnimationSystem
    {
        bool StartTransition(VisualElement owner, StylePropertyId prop, float startValue, float endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, int startValue, int endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Length startValue, Length endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Color startValue, Color endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartAnimationEnum(VisualElement owner, StylePropertyId prop, int startValue, int endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Background startValue, Background endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, FontDefinition startValue, FontDefinition endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Font startValue, Font endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Cursor startValue, Cursor endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, TextShadow startValue, TextShadow endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Scale startValue, Scale endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, TransformOrigin startValue, TransformOrigin endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Translate startValue, Translate endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        bool StartTransition(VisualElement owner, StylePropertyId prop, Rotate startValue, Rotate endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve);
        void CancelAllAnimations();
        void CancelAllAnimations(VisualElement owner);
        void CancelAnimation(VisualElement owner, StylePropertyId id);
        bool HasRunningAnimation(VisualElement owner, StylePropertyId id);
        void UpdateAnimation(VisualElement owner, StylePropertyId id);
        void GetAllAnimations(VisualElement owner, List<StylePropertyId> propertyIds);
        void Update();
    }

    internal class StylePropertyAnimationSystem : IStylePropertyAnimationSystem
    {
        [Flags]
        private enum TransitionState
        {
            None = 0,
            Running = 1 << 0,
            Started = 1 << 1,
            Ended = 1 << 2,
            Canceled = 1 << 3
        }

        private long m_CurrentTimeMs = 0;

        private struct AnimationDataSet<TTimingData, TStyleData>
        {
            private const int InitialSize = 2;

            // Using a struct-of-arrays pattern to make this easier to migrate to NativeArray + Job system eventually.

            // Running animations
            public VisualElement[] elements;
            public StylePropertyId[] properties;
            public TTimingData[] timing;
            public TStyleData[] style;
            public int count;

            // An [(element, property) -> index] lookup for O(1) IndexOf.
            private Dictionary<ElementPropertyPair, int> indices;

            private int capacity
            {
                get => elements.Length;
                set
                {
                    Array.Resize(ref elements, value);
                    Array.Resize(ref properties, value);
                    Array.Resize(ref timing, value);
                    Array.Resize(ref style, value);
                }
            }

            private void LocalInit()
            {
                elements = new VisualElement[InitialSize];
                properties = new StylePropertyId[InitialSize];
                timing = new TTimingData[InitialSize];
                style = new TStyleData[InitialSize];
                indices = new Dictionary<ElementPropertyPair, int>(ElementPropertyPair.Comparer);
            }

            public static AnimationDataSet<TTimingData, TStyleData> Create()
            {
                var result = new AnimationDataSet<TTimingData, TStyleData>();
                result.LocalInit();
                return result;
            }

            public bool IndexOf(VisualElement ve, StylePropertyId prop, out int index)
            {
                return indices.TryGetValue(new ElementPropertyPair(ve, prop), out index);
            }

            public void Add(VisualElement owner, StylePropertyId prop, TTimingData timingData,
                TStyleData styleData)
            {
                if (count >= capacity)
                {
                    capacity *= 2;
                }

                int index = count++;
                elements[index] = owner;
                properties[index] = prop;
                timing[index] = timingData;
                style[index] = styleData;
                indices.Add(new ElementPropertyPair(owner, prop), index);
            }

            public void Remove(int cancelledIndex)
            {
                int lastIndex = --count;

                indices.Remove(new ElementPropertyPair(elements[cancelledIndex], properties[cancelledIndex]));

                if (cancelledIndex != lastIndex)
                {
                    var movedElement = elements[cancelledIndex] = elements[lastIndex];
                    var movedProperty = properties[cancelledIndex] = properties[lastIndex];
                    timing[cancelledIndex] = timing[lastIndex];
                    style[cancelledIndex] = style[lastIndex];
                    indices[new ElementPropertyPair(movedElement, movedProperty)] = cancelledIndex;
                }

                elements[lastIndex] = default;
                properties[lastIndex] = default;
                timing[lastIndex] = default;
                style[lastIndex] = default;
            }

            public void Replace(int index, TTimingData timingData, TStyleData styleData)
            {
                timing[index] = timingData;
                style[index] = styleData;
            }

            public void RemoveAll(VisualElement ve)
            {
                int n = count;
                for (var i = n - 1; i >= 0; i--)
                {
                    if (elements[i] == ve)
                        Remove(i);
                }
            }

            public void RemoveAll()
            {
                capacity = InitialSize;
                var usedSize = Mathf.Min(count, capacity);
                Array.Clear(elements, 0, usedSize);
                Array.Clear(properties, 0, usedSize);
                Array.Clear(timing, 0, usedSize);
                Array.Clear(style, 0, usedSize);
                count = 0;
                indices.Clear();
            }

            public void GetActivePropertiesForElement(VisualElement ve, List<StylePropertyId> outProperties)
            {
                int n = count;
                for (var i = n - 1; i >= 0; i--)
                {
                    if (elements[i] == ve)
                        outProperties.Add(properties[i]);
                }
            }
        }

        private struct ElementPropertyPair
        {
            public static readonly IEqualityComparer<ElementPropertyPair> Comparer = new EqualityComparer();

            public readonly VisualElement element;
            public readonly StylePropertyId property;

            public ElementPropertyPair(VisualElement element, StylePropertyId property)
            {
                this.element = element;
                this.property = property;
            }

            private class EqualityComparer : IEqualityComparer<ElementPropertyPair>
            {
                public bool Equals(ElementPropertyPair x, ElementPropertyPair y)
                {
                    return x.element == y.element && x.property == y.property;
                }

                public int GetHashCode(ElementPropertyPair obj)
                {
                    unchecked
                    {
                        return (obj.element.GetHashCode() * 397) ^ (int)obj.property;
                    }
                }
            }
        }

        abstract class Values
        {
            public abstract void CancelAllAnimations();
            public abstract void CancelAllAnimations(VisualElement ve);
            public abstract void CancelAnimation(VisualElement ve, StylePropertyId id);
            public abstract bool HasRunningAnimation(VisualElement ve, StylePropertyId id);
            public abstract void UpdateAnimation(VisualElement ve, StylePropertyId id);
            public abstract void GetAllAnimations(VisualElement ve, List<StylePropertyId> outPropertyIds);
            public abstract void Update(long currentTimeMs);
            protected abstract void UpdateValues();
            protected abstract void UpdateComputedStyle();
            protected abstract void UpdateComputedStyle(int i);
        }

        abstract class Values<T> : Values
        {
            private long m_CurrentTimeMs = 0;
            private class TransitionEventsFrameState
            {
                private static readonly UnityEngine.Pool.ObjectPool<Queue<EventBase>> k_EventQueuePool = new UnityEngine.Pool.ObjectPool<Queue<EventBase>>(() => new Queue<EventBase>(4));

                // Contains the transition state that changed during the frame.
                public readonly Dictionary<ElementPropertyPair, TransitionState> elementPropertyStateDelta = new Dictionary<ElementPropertyPair, TransitionState>(ElementPropertyPair.Comparer);
                // Contains the events that were queued during the frame, which are collapsed if needed when QueueTransitionCancelEvent is called.
                public readonly Dictionary<ElementPropertyPair, Queue<EventBase>> elementPropertyQueuedEvents = new Dictionary<ElementPropertyPair, Queue<EventBase>>(ElementPropertyPair.Comparer);
                public IPanel panel;

                private int m_ChangesCount;

                public static Queue<EventBase> GetPooledQueue()
                {
                    return k_EventQueuePool.Get();
                }

                public void RegisterChange()
                {
                    m_ChangesCount++;
                }

                public void UnregisterChange()
                {
                    m_ChangesCount--;
                }

                public bool StateChanged()
                {
                    return m_ChangesCount > 0;
                }

                public void Clear()
                {
                    foreach (var kvp in elementPropertyQueuedEvents)
                    {
                        elementPropertyStateDelta[kvp.Key] = TransitionState.None;
                        kvp.Value.Clear();
                        k_EventQueuePool.Release(kvp.Value);
                    }
                    elementPropertyQueuedEvents.Clear();
                    panel = null;
                    m_ChangesCount = 0;
                }
            }

            private TransitionEventsFrameState m_CurrentFrameEventsState = new TransitionEventsFrameState();
            private TransitionEventsFrameState m_NextFrameEventsState = new TransitionEventsFrameState();

            public struct TimingData
            {
                public long startTimeMs;
                public int durationMs;
                public Func<float, float> easingCurve;
                public float easedProgress;
                public float reversingShorteningFactor;
                public bool isStarted;
                public int delayMs;
            }

            public struct StyleData
            {
                public T startValue;
                public T endValue;
                public T reversingAdjustedStartValue;
                public T currentValue;
            }

            public struct EmptyData
            {
                public static EmptyData Default = default;
            }

            public AnimationDataSet<TimingData, StyleData> running;
            public AnimationDataSet<EmptyData, T> completed;
            public bool isEmpty => running.count + completed.count == 0;

            public abstract Func<T, T, bool> SameFunc { get; }

            protected Values()
            {
                running = AnimationDataSet<TimingData, StyleData>.Create();
                completed = AnimationDataSet<EmptyData, T>.Create();
                m_CurrentTimeMs = Panel.TimeSinceStartupMs();
            }

            private void SwapFrameStates()
            {
                TransitionEventsFrameState temp = m_CurrentFrameEventsState;
                m_CurrentFrameEventsState = m_NextFrameEventsState;
                m_NextFrameEventsState = temp;
            }

            private void QueueEvent(EventBase evt, ElementPropertyPair epp)
            {
                evt.target = epp.element;
                Queue<EventBase> queue;

                if (!m_NextFrameEventsState.elementPropertyQueuedEvents.TryGetValue(epp, out queue))
                {
                    queue = TransitionEventsFrameState.GetPooledQueue();
                    m_NextFrameEventsState.elementPropertyQueuedEvents.Add(epp, queue);
                }
                queue.Enqueue(evt);

                if (m_NextFrameEventsState.panel == null)
                    m_NextFrameEventsState.panel = epp.element.panel;
                m_NextFrameEventsState.RegisterChange();
            }

            private void ClearEventQueue(ElementPropertyPair epp)
            {
                Queue<EventBase> queue;
                if (m_NextFrameEventsState.elementPropertyQueuedEvents.TryGetValue(epp, out queue))
                {
                    while (queue.Count > 0)
                    {
                        queue.Dequeue().Dispose();
                        m_NextFrameEventsState.UnregisterChange();
                    }
                }
            }

            private void QueueTransitionRunEvent(VisualElement ve, int runningIndex)
            {
                ref var timingData = ref running.timing[runningIndex];
                var stylePropertyId = running.properties[runningIndex];
                var elapsedTimeMs = timingData.delayMs < 0 ? Mathf.Min(Mathf.Max(-timingData.delayMs, 0), timingData.durationMs) : 0;
                var epp = new ElementPropertyPair(ve, stylePropertyId);
                var evt = TransitionRunEvent.GetPooled(new StylePropertyName(stylePropertyId), elapsedTimeMs / 1000.0f);

                if (m_NextFrameEventsState.elementPropertyStateDelta.ContainsKey(epp))
                    m_NextFrameEventsState.elementPropertyStateDelta[epp] |= TransitionState.Running;
                else
                    m_NextFrameEventsState.elementPropertyStateDelta.Add(epp, TransitionState.Running);

                QueueEvent(evt, epp);
            }

            private void QueueTransitionStartEvent(VisualElement ve, int runningIndex)
            {
                ref var timingData = ref running.timing[runningIndex];
                var stylePropertyId = running.properties[runningIndex];
                var elapsedTimeMs = timingData.delayMs < 0 ? Mathf.Min(Mathf.Max(-timingData.delayMs, 0), timingData.durationMs) : 0;
                var epp = new ElementPropertyPair(ve, stylePropertyId);
                var evt = TransitionStartEvent.GetPooled(new StylePropertyName(stylePropertyId), elapsedTimeMs / 1000.0f);

                if (m_NextFrameEventsState.elementPropertyStateDelta.ContainsKey(epp))
                    m_NextFrameEventsState.elementPropertyStateDelta[epp] |= TransitionState.Started;
                else
                    m_NextFrameEventsState.elementPropertyStateDelta.Add(epp, TransitionState.Started);

                QueueEvent(evt, epp);
            }

            private void QueueTransitionEndEvent(VisualElement ve, int runningIndex)
            {
                ref var timingData = ref running.timing[runningIndex];
                var stylePropertyId = running.properties[runningIndex];
                var epp = new ElementPropertyPair(ve, stylePropertyId);
                var evt = TransitionEndEvent.GetPooled(new StylePropertyName(stylePropertyId), timingData.durationMs / 1000.0f);

                if (m_NextFrameEventsState.elementPropertyStateDelta.ContainsKey(epp))
                    m_NextFrameEventsState.elementPropertyStateDelta[epp] |= TransitionState.Ended;
                else
                    m_NextFrameEventsState.elementPropertyStateDelta.Add(epp, TransitionState.Ended);

                QueueEvent(evt, epp);
            }

            private void QueueTransitionCancelEvent(VisualElement ve, int runningIndex, long panelElapsedMs)
            {
                ref var timingData = ref running.timing[runningIndex];
                var stylePropertyId = running.properties[runningIndex];
                var elapsedTimeMs = timingData.isStarted ? panelElapsedMs - timingData.startTimeMs : 0;
                var epp = new ElementPropertyPair(ve, stylePropertyId);

                if (timingData.delayMs < 0)
                {
                    elapsedTimeMs = -timingData.delayMs + elapsedTimeMs;
                }

                var evt = TransitionCancelEvent.GetPooled(new StylePropertyName(stylePropertyId), elapsedTimeMs / 1000.0f);

                if (m_NextFrameEventsState.elementPropertyStateDelta.ContainsKey(epp))
                {
                    // Delta is empty, set delta to Cancel, OR
                    // Delta already contains Cancel, set delta to Cancel (removing run and start from the delta).
                    // e.g. (cancel, run, start) + (cancel) = (cancel)
                    if (m_NextFrameEventsState.elementPropertyStateDelta[epp] == TransitionState.None ||
                        (m_NextFrameEventsState.elementPropertyStateDelta[epp] & TransitionState.Canceled) == TransitionState.Canceled)
                    {
                        m_NextFrameEventsState.elementPropertyStateDelta[epp] = TransitionState.Canceled;
                        ClearEventQueue(epp);
                        QueueEvent(evt, epp);
                    }
                    // Delta contains something but not Cancel, clearing delta.
                    else
                    {
                        m_NextFrameEventsState.elementPropertyStateDelta[epp] = TransitionState.None;
                        ClearEventQueue(epp);
                    }
                }
                else
                {
                    m_NextFrameEventsState.elementPropertyStateDelta.Add(epp, TransitionState.Canceled);
                    QueueEvent(evt, epp);
                }
            }

            private void SendTransitionCancelEvent(VisualElement ve, int runningIndex, long panelElapsedMs)
            {
                ref var timingData = ref running.timing[runningIndex];
                var stylePropertyId = running.properties[runningIndex];
                var elapsedTimeMs = timingData.isStarted ? panelElapsedMs - timingData.startTimeMs : 0;

                if (timingData.delayMs < 0)
                {
                    elapsedTimeMs = -timingData.delayMs + elapsedTimeMs;
                }

                using (var evt = TransitionCancelEvent.GetPooled(new StylePropertyName(stylePropertyId), elapsedTimeMs / 1000.0f))
                {
                    evt.target = ve;
                    ve.SendEvent(evt);
                }
            }

            public sealed override void CancelAllAnimations()
            {
                var runningCount = running.count;
                if (runningCount > 0)
                {
                    // All running.elements are in the same Panel, thus we can use the first one to gate the EventDispatcher.
                    using (new EventDispatcherGate(running.elements[0].panel.dispatcher))
                    {
                        for (int i = 0; i < runningCount; ++i)
                        {
                            var ve = running.elements[i];
                            // We send the event instantly instead of queuing it in the case of a panel change, to make sure it is sent while the panel is still the old one.
                            SendTransitionCancelEvent(ve, i, m_CurrentTimeMs);
                            ForceComputedStyleEndValue(i);
                            ve.styleAnimation.runningAnimationCount--;
                        }
                    }

                    running.RemoveAll();
                }

                var completedCount = completed.count;
                for (var i = 0; i < completedCount; ++i)
                {
                    var ve = completed.elements[i];
                    ve.styleAnimation.completedAnimationCount--;
                }
                completed.RemoveAll();
            }

            public sealed override void CancelAllAnimations(VisualElement ve)
            {
                int count = running.count;

                if (count > 0)
                {
                    // Loop forward to send the events in the proper order, even though it means we have to loop twice through the running data set.
                    using (new EventDispatcherGate(running.elements[0].panel.dispatcher))
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            if (running.elements[i] == ve)
                            {
                                // We send the event instantly instead of queuing it in the case of a panel change, to make sure it is sent while the panel is still the old one.
                                SendTransitionCancelEvent(ve, i, m_CurrentTimeMs);
                                ForceComputedStyleEndValue(i);
                                running.elements[i].styleAnimation.runningAnimationCount--;
                            }
                        }
                    }
                }

                running.RemoveAll(ve);

                var completedCount = completed.count;
                for (int i = 0; i < completedCount; i++)
                {
                    if (completed.elements[i] == ve)
                    {
                        completed.elements[i].styleAnimation.completedAnimationCount--;
                    }
                }
                completed.RemoveAll(ve);
            }

            public sealed override void CancelAnimation(VisualElement ve, StylePropertyId id)
            {
                if (running.IndexOf(ve, id, out int runningIndex))
                {
                    QueueTransitionCancelEvent(ve, runningIndex, m_CurrentTimeMs);
                    ForceComputedStyleEndValue(runningIndex);
                    running.Remove(runningIndex);
                    ve.styleAnimation.runningAnimationCount--;
                }

                if (completed.IndexOf(ve, id, out int completedIndex))
                {
                    completed.Remove(completedIndex);
                    ve.styleAnimation.completedAnimationCount--;
                }
            }

            public sealed override bool HasRunningAnimation(VisualElement ve, StylePropertyId id)
            {
                return running.IndexOf(ve, id, out _);
            }

            public sealed override void UpdateAnimation(VisualElement ve, StylePropertyId id)
            {
                if (running.IndexOf(ve, id, out int runningIndex))
                    UpdateComputedStyle(runningIndex);
            }

            public sealed override void GetAllAnimations(VisualElement ve, List<StylePropertyId> outPropertyIds)
            {
                running.GetActivePropertiesForElement(ve, outPropertyIds);
                completed.GetActivePropertiesForElement(ve, outPropertyIds);
            }

            private float ComputeReversingShorteningFactor(int oldIndex)
            {
                ref var timingData = ref running.timing[oldIndex];
                return Mathf.Clamp01(
                    Mathf.Abs(1 - (1 - timingData.easedProgress) * timingData.reversingShorteningFactor));
            }

            private int ComputeReversingDuration(int newTransitionDurationMs, float newReversingShorteningFactor)
            {
                return Mathf.RoundToInt(newTransitionDurationMs * newReversingShorteningFactor);
            }

            private int ComputeReversingDelay(int delayMs, float newReversingShorteningFactor)
            {
                return delayMs < 0 ? Mathf.RoundToInt(delayMs * newReversingShorteningFactor) : delayMs;
            }

            // See https://drafts.csswg.org/css-transitions/#starting for W3 specs.
            // Start or update the values for the style animation.
            // Returns true if a transition animation is created, that is,
            // if computed style doesn't need to be updated directly to the new style.
            public bool StartTransition(VisualElement owner, StylePropertyId prop, T startValue, T endValue,
                int durationMs, int delayMs, Func<float, float> easingCurve, long currentTimeMs)
            {
                long startTimeMs = currentTimeMs + delayMs;

                var timing = new TimingData
                {
                    startTimeMs = startTimeMs,
                    durationMs = durationMs,
                    easingCurve = easingCurve,
                    reversingShorteningFactor = 1f,
                    delayMs = delayMs
                };
                var style = new StyleData
                {
                    startValue = startValue,
                    endValue = endValue,
                    currentValue = startValue,
                    reversingAdjustedStartValue = startValue
                };

                int combinedDuration = Mathf.Max(0, durationMs) + delayMs;

                // There was a prior completed animation
                if (completed.IndexOf(owner, prop, out var completedIndex))
                {
                    // 1. If all of the following are true:
                    // - the element does not have a completed transition for the property or the end value of the
                    //   completed transition is different from the after-change style for the property,
                    if (SameFunc(endValue, completed.style[completedIndex]))
                    {
                        return false;
                    }

                    // 1. If all of the following are true:
                    // - the combined duration is greater than 0s,
                    if (combinedDuration <= 0)
                    {
                        return false;
                    }

                    // 2. If the element has a completed transition for the property and the end value of the completed
                    // transition is different from the after-change style for the property, then implementations must
                    // remove the completed transition from the set of completed transitions.
                    completed.Remove(completedIndex);
                    owner.styleAnimation.completedAnimationCount--;
                }

                // Existing animation? See if we can retarget the new one to better fit the old one.
                if (running.IndexOf(owner, prop, out var index))
                {
                    // 4. If the element has a running transition for the property, there is a matching transition-
                    // property value, and the end value of the running transition is not equal to the value of the
                    // property in the after-change style, then:
                    if (SameFunc(endValue, running.style[index].endValue))
                    {
                        return false;
                    }

                    // 4.1. If the current value of the property in the running transition is equal to the value of the
                    // property in the after-change style then implementations must cancel the running transition.
                    if (SameFunc(endValue, running.style[index].currentValue))
                    {
                        QueueTransitionCancelEvent(owner, index, currentTimeMs);
                        running.Remove(index);
                        owner.styleAnimation.runningAnimationCount--;
                        return false;
                    }

                    // 4.2. Otherwise, if the combined duration is less than or equal to 0s [...],
                    // then implementations must cancel the running transition.
                    if (combinedDuration <= 0)
                    {
                        QueueTransitionCancelEvent(owner, index, currentTimeMs);
                        running.Remove(index);
                        owner.styleAnimation.runningAnimationCount--;

                        return false;
                    }

                    style.startValue = running.style[index].currentValue;
                    style.currentValue = style.startValue;

                    // 4.3 Otherwise, if the reversing-adjusted start value of the running transition is the same as
                    // the value of the property in the after-change style, implementations must cancel the running
                    // transition and start a new transition whose reversing-adjusted start value is the end value
                    // of the running transition, [...]
                    if (SameFunc(endValue, running.style[index].startValue))
                    {
                        float rsf = timing.reversingShorteningFactor = ComputeReversingShorteningFactor(index);
                        timing.startTimeMs = currentTimeMs + ComputeReversingDelay(delayMs, rsf);
                        timing.durationMs = ComputeReversingDuration(durationMs, rsf);
                        style.reversingAdjustedStartValue = running.style[index].endValue;
                    }

                    running.timing[index].isStarted = false;
                    QueueTransitionCancelEvent(owner, index, currentTimeMs);
                    QueueTransitionRunEvent(owner, index);
                    running.Replace(index, timing, style);
                    return true;
                }

                // According to the W3 standard, 0-duration anims don't exist, and simply don't start a transition.
                // 1. If all of the following are true:
                // - the combined duration is greater than 0s,
                if (combinedDuration <= 0)
                    return false;

                // 1. If all of the following are true:
                // - the before-change style is different from the after-change style for that property
                if (SameFunc(startValue, endValue))
                    return false;

                // If we reached this point, then all the criteria are satisfied to start a new animation.
                // Note that animations that no longer have a matching transition-property will be cancelled
                // by the style updating system, so we don't need to account for that here.

                running.Add(owner, prop, timing, style);
                owner.styleAnimation.runningAnimationCount++;
                QueueTransitionRunEvent(owner, running.count - 1);

                return true;
            }

            private void ForceComputedStyleEndValue(int runningIndex)
            {
                // Force ComputedStyle to endValue immediately (used when cancelling animations).
                ref var style = ref running.style[runningIndex];
                style.currentValue = style.endValue;
                UpdateComputedStyle(runningIndex);
            }

            public sealed override void Update(long currentTimeMs)
            {
                m_CurrentTimeMs = currentTimeMs;
                UpdateProgress(currentTimeMs);
                UpdateValues();
                UpdateComputedStyle();
                if (m_NextFrameEventsState.StateChanged())
                    ProcessEventQueue();
            }

            private void ProcessEventQueue()
            {
                SwapFrameStates();

                EventDispatcher d = m_CurrentFrameEventsState.panel?.dispatcher;
                using (new EventDispatcherGate(d))
                {
                    foreach (var kvp in m_CurrentFrameEventsState.elementPropertyQueuedEvents)
                    {
                        var epp = kvp.Key;
                        var queue = kvp.Value;
                        var element = kvp.Key.element;

                        while (queue.Count > 0)
                        {
                            var evt = queue.Dequeue();
                            element.SendEvent(evt);
                            evt.Dispose();
                        }
                    }
                    m_CurrentFrameEventsState.Clear();
                }
            }

            private void UpdateProgress(long currentTimeMs)
            {
                int n = running.count;
                if (n > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        ref var timing = ref running.timing[i];
                        if (currentTimeMs < timing.startTimeMs)
                        {
                            // We implement transition delay by running the animation and forcing the interpolation to be
                            // frozen at the start value for the duration of the delay. This might not conform entirely
                            // with the W3 standard, but there is no external system to support and test this property
                            // so this is a reasonable start.
                            timing.easedProgress = 0;
                        }
                        else if (currentTimeMs >= timing.startTimeMs + timing.durationMs)
                        {
                            ref var style = ref running.style[i];
                            ref var owner = ref running.elements[i];

                            style.currentValue =
                                style.endValue; // Force end value no matter what the easing curve says.
                            UpdateComputedStyle(i);
                            completed.Add(owner, running.properties[i], EmptyData.Default, style.endValue);
                            owner.styleAnimation.runningAnimationCount--;
                            owner.styleAnimation.completedAnimationCount++;

                            QueueTransitionEndEvent(owner, i);
                            running.Remove(i);

                            i--;
                            n--;
                        }
                        else
                        {
                            if (!timing.isStarted)
                            {
                                timing.isStarted = true;
                                QueueTransitionStartEvent(running.elements[i], i);
                            }

                            var progress = (currentTimeMs - timing.startTimeMs) / (float)timing.durationMs;
                            timing.easedProgress = timing.easingCurve(progress);
                        }
                    }
                }
            }
        }

        class ValuesFloat : Values<float>
        {
            public override Func<float, float, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(float a, float b) => Mathf.Approximately(a, b);
            private static float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }

            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesInt : Values<int>
        {
            public override Func<int, int, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(int a, int b) => a == b;
            private static int Lerp(int a, int b, float t) => Mathf.RoundToInt(Mathf.LerpUnclamped(a, b, t));

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }

            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesLength : Values<Length>
        {
            public override Func<Length, Length, bool> SameFunc { get; } = IsSame;

            private static bool IsSame(Length a, Length b) => a.unit == b.unit && Mathf.Approximately(a.value, b.value);

            internal static Length Lerp(Length a, Length b, float t) =>
                new Length(Mathf.LerpUnclamped(a.value, b.value, t), b.unit);

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }

            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesColor : Values<Color>
        {
            public override Func<Color, Color, bool> SameFunc { get; } = IsSame;

            private static bool IsSame(Color c, Color d) =>
                Mathf.Approximately(c.r, d.r) && Mathf.Approximately(c.g, d.g) &&
                Mathf.Approximately(c.b, d.b) && Mathf.Approximately(c.a, d.a);

            private static Color Lerp(Color a, Color b, float t) => Color.LerpUnclamped(a, b, t);

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }

            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        abstract class ValuesDiscrete<T> : Values<T>
        {
            public override Func<T, T, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
            private static T Lerp(T a, T b, float t) => t < 0.5f ? a : b;

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }
        }

        class ValuesEnum : ValuesDiscrete<int>
        {
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesBackground : ValuesDiscrete<Background>
        {
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesFontDefinition : ValuesDiscrete<FontDefinition>
        {
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesFont : ValuesDiscrete<Font>
        {
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesCursor : ValuesDiscrete<Cursor>
        {
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesTextShadow : Values<TextShadow>
        {
            public override Func<TextShadow, TextShadow, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(TextShadow a, TextShadow b) => a == b;
            private static TextShadow Lerp(TextShadow a, TextShadow b, float t) => TextShadow.LerpUnclamped(a, b, t);

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }

            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }
        }

        class ValuesScale : Values<Scale>
        {
            public override Func<Scale, Scale, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(Scale a, Scale b) => a == b;
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }

            private static Scale Lerp(Scale a, Scale b, float t) => new Scale(Vector3.LerpUnclamped(a.value, b.value, t));


            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }
        }

        class ValuesRotate : Values<Rotate>
        {
            public override Func<Rotate, Rotate, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(Rotate a, Rotate b) => a == b;
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }

            //TODO axis is not "lerped" as it is assumed to be z only.
            private static Rotate Lerp(Rotate a, Rotate b, float t) => new Rotate(Mathf.LerpUnclamped(a.angle.ToDegrees(), b.angle.ToDegrees(), t));

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }
        }

        class ValuesTranslate : Values<Translate>
        {
            public override Func<Translate, Translate, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(Translate a, Translate b) => a == b;

            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }

            private static Translate Lerp(Translate a, Translate b, float t) => new Translate(ValuesLength.Lerp(a.x, b.x, t), ValuesLength.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t));

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }
        }

        class ValuesTransformOrigin : Values<TransformOrigin>
        {
            public override Func<TransformOrigin, TransformOrigin, bool> SameFunc { get; } = IsSame;
            private static bool IsSame(TransformOrigin a, TransformOrigin b) => a == b;
            protected sealed override void UpdateComputedStyle()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                        running.properties[i], running.style[i].currentValue);
                }
            }

            protected sealed override void UpdateComputedStyle(int i)
            {
                running.elements[i].computedStyle.ApplyPropertyAnimation(running.elements[i],
                    running.properties[i], running.style[i].currentValue);
            }

            private static TransformOrigin Lerp(TransformOrigin a, TransformOrigin b, float t) => new TransformOrigin(ValuesLength.Lerp(a.x, b.x, t), ValuesLength.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t));

            protected sealed override void UpdateValues()
            {
                int n = running.count;
                for (int i = 0; i < n; i++)
                {
                    ref var timing = ref running.timing[i];
                    ref var style = ref running.style[i];
                    style.currentValue = Lerp(style.startValue, style.endValue, timing.easedProgress);
                }
            }
        }

        private ValuesFloat m_Floats;
        private ValuesInt m_Ints;
        private ValuesLength m_Lengths;
        private ValuesColor m_Colors;
        private ValuesEnum m_Enums;
        private ValuesBackground m_Backgrounds;
        private ValuesFontDefinition m_FontDefinitions;
        private ValuesFont m_Fonts;
        private ValuesCursor m_Cursors;
        private ValuesTextShadow m_TextShadows;
        private ValuesScale m_Scale;
        private ValuesRotate m_Rotate;
        private ValuesTranslate m_Translate;
        private ValuesTransformOrigin m_TransformOrigin;

        // All the value lists with ongoing animations. Add and remove Values objects when animations come in/out.
        private readonly List<Values> m_AllValues = new List<Values>();

        public StylePropertyAnimationSystem()
        {
            m_CurrentTimeMs = Panel.TimeSinceStartupMs();
        }

        private T GetOrCreate<T>(ref T values) where T : new()
        {
            return values ?? (values = new T());
        }

        private readonly Dictionary<StylePropertyId, Values> m_PropertyToValues = new Dictionary<StylePropertyId, Values>();

        // Start or update the values for the style animation
        private bool StartTransition<T>(VisualElement owner, StylePropertyId prop, T startValue, T endValue,
            int durationMs, int delayMs, Func<float, float> easingCurve, Values<T> values)
        {
            m_PropertyToValues[prop] = values;
            var result = values.StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, CurrentTimeMs());
            UpdateTracking(values);
            return result;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, float startValue, float endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Floats));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, int startValue, int endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Ints));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Length startValue, Length endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Lengths));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Color startValue, Color endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Colors));
        }

        public bool StartAnimationEnum(VisualElement owner, StylePropertyId prop, int startValue, int endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Enums));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Background startValue, Background endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Backgrounds));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, FontDefinition startValue, FontDefinition endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_FontDefinitions));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Font startValue, Font endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Fonts));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Cursor startValue, Cursor endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Cursors));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, TextShadow startValue, TextShadow endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_TextShadows));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Scale startValue, Scale endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Scale));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Rotate startValue, Rotate endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Rotate));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Translate startValue, Translate endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_Translate));
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, TransformOrigin startValue, TransformOrigin endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return StartTransition(owner, prop, startValue, endValue, durationMs, delayMs, easingCurve, GetOrCreate(ref m_TransformOrigin));
        }

        public void CancelAllAnimations()
        {
            foreach (var values in m_AllValues)
            {
                values.CancelAllAnimations();
            }
        }

        public void CancelAllAnimations(VisualElement owner)
        {
            foreach (var values in m_AllValues)
            {
                values.CancelAllAnimations(owner);
            }

            Assert.AreEqual(0, owner.styleAnimation.runningAnimationCount);
            Assert.AreEqual(0, owner.styleAnimation.completedAnimationCount);
        }

        public void CancelAnimation(VisualElement owner, StylePropertyId id)
        {
            // For performance considerations, we anticipate that the styling system could be calling specialized
            // versions (CancelAnimationFloat, CancelAnimationColor, etc.) instead of this in the future.
            if (m_PropertyToValues.TryGetValue(id, out var values))
                values.CancelAnimation(owner, id);
        }

        public bool HasRunningAnimation(VisualElement owner, StylePropertyId id)
        {
            return m_PropertyToValues.TryGetValue(id, out var values) && values.HasRunningAnimation(owner, id);
        }

        public void UpdateAnimation(VisualElement owner, StylePropertyId id)
        {
            if (m_PropertyToValues.TryGetValue(id, out var values))
                values.UpdateAnimation(owner, id);
        }

        public void GetAllAnimations(VisualElement owner, List<StylePropertyId> propertyIds)
        {
            foreach (var values in m_AllValues)
                values.GetAllAnimations(owner, propertyIds);
        }

        private void UpdateTracking<T>(Values<T> values)
        {
            // Register new type of animations to keep track of. Note that we don't unregister if values becomes empty.
            if (!values.isEmpty && !m_AllValues.Contains(values))
            {
                m_AllValues.Add(values);
            }
        }

        long CurrentTimeMs()
        {
            return m_CurrentTimeMs;
        }

        public void Update()
        {
            m_CurrentTimeMs = Panel.TimeSinceStartupMs();
            var count = m_AllValues.Count;
            for (int i = 0; i < count; i++)
            {
                m_AllValues[i].Update(m_CurrentTimeMs);
            }
        }
    }

    internal class EmptyStylePropertyAnimationSystem : IStylePropertyAnimationSystem
    {
        public bool StartTransition(VisualElement owner, StylePropertyId prop, float startValue, float endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, int startValue, int endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Length startValue, Length endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Color startValue, Color endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartAnimationEnum(VisualElement owner, StylePropertyId prop, int startValue, int endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Background startValue, Background endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, FontDefinition startValue, FontDefinition endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Font startValue, Font endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Cursor startValue, Cursor endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, TextShadow startValue, TextShadow endValue, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Scale startValue, Scale endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, TransformOrigin startValue, TransformOrigin endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Translate startValue, Translate endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return false;
        }

        public bool StartTransition(VisualElement owner, StylePropertyId prop, Rotate startValue, Rotate endValue, int durationMs, int delayMs, [NotNull] Func<float, float> easingCurve)
        {
            return false;
        }

        public void CancelAllAnimations()
        {
        }

        public void CancelAllAnimations(VisualElement owner)
        {
        }

        public void CancelAnimation(VisualElement owner, StylePropertyId id)
        {
        }

        public bool HasRunningAnimation(VisualElement owner, StylePropertyId id)
        {
            return false;
        }

        public void UpdateAnimation(VisualElement owner, StylePropertyId id)
        {
        }

        public void GetAllAnimations(VisualElement owner, List<StylePropertyId> propertyIds)
        {
        }

        public void Update()
        {
        }
    }
}
