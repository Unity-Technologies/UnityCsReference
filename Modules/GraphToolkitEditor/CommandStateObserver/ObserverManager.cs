// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.Profiling;
using UnityEngine;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// A class responsible for notifying observers when the state they observe is dirtied.
    /// </summary>
    [UnityRestricted]
    internal sealed class ObserverManager
    {
        /// <summary>
        /// A mapping of state component to observers observing those components.
        /// </summary>
        readonly Dictionary<IStateComponent, List<IStateObserver>> m_StateObservers = new();

        List<IStateObserver> m_SortedObservers = new();
        readonly List<IStateObserver> m_ObserverCallList = new();
        readonly HashSet<IStateComponent> m_DirtyComponentSet = new();
        readonly List<IStateObserver> m_NewObservers = new();

        int m_MarkerUniqueId;
        Dictionary<IStateObserver, ProfilerMarker> m_ProfilerMarkers = new();

        /// <summary>
        /// Returns true if the observers are being notified.
        /// </summary>
        bool IsObserving { get; set; }

        /// <summary>
        /// Registers a state observer to the manager.
        /// </summary>
        /// <remarks>
        /// If the content of <see cref="StateObserver.ObservedStateComponents"/> is modified after
        /// the observer is registered, the observer should be unregistered and registered again.
        /// </remarks>
        /// <param name="observer">The observer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the observer is already registered.</exception>
        public void RegisterObserver(IStateObserver observer)
        {
            if (observer == null)
                return;

            m_NewObservers.Add(observer);
            m_ProfilerMarkers[observer] = new ProfilerMarker($"{observer.GetType()}_{m_MarkerUniqueId++}.Observe");

            foreach (var component in observer.ObservedStateComponents)
            {
                if (!m_StateObservers.TryGetValue(component, out var observerForComponent))
                {
                    observerForComponent = new List<IStateObserver>();
                    m_StateObservers[component] = observerForComponent;
                }

                if (observerForComponent.Contains(observer))
                    throw new InvalidOperationException("Cannot register the same observer twice.");

                observerForComponent.Add(observer);
                m_SortedObservers = null;
            }
        }

        /// <summary>
        /// Unregisters a state observer from the manager.
        /// </summary>
        /// <param name="observer">The observer.</param>
        public void UnregisterObserver(IStateObserver observer)
        {
            if (observer == null)
                return;

            // We do not loop on observer.ObservedStateComponents here,
            // in case observer.ObservedStateComponents changed since RegisterObserver() was called.
            foreach (var observersByComponent in m_StateObservers)
            {
                observersByComponent.Value.Remove(observer);
                m_SortedObservers = null;
            }

            m_NewObservers.Remove(observer);
            m_ProfilerMarkers.Remove(observer);
        }

        static bool ContainsMoreThan<T>(IReadOnlyList<T> container, T value, int limit)
        {
            if (container.Count <= limit)
                return false;

            var comp = EqualityComparer<T>.Default;
            var count = 0;
            foreach (var element in container)
            {
                if (comp.Equals(element, value))
                {
                    count++;
                }

                if (count > limit)
                    return true;
            }

            return false;
        }

        void SortObservers()
        {
            // Observers and the state components they observe/modify form a dependency graph: if observer A observe a
            // component that is modified by observer B, observer A depends on observer B.
            // We want to call the observers in an order that respects these dependencies.

            var observers = new List<IStateObserver>();

            foreach (var observerList in m_StateObservers.Values)
            {
                observers.AddFewDistinct(observerList);
            }

            m_SortedObservers = new List<IStateObserver>(observers.Count);
            SortObservers(observers, m_SortedObservers);
        }

        // Will modify observersToSort.
        // Internal for tests.
        internal static void SortObservers(List<IStateObserver> observersToSort, List<IStateObserver> sortedObservers)
        {
            var endBatch = new List<IStateObserver>(observersToSort.Count);

            // In general, loops in the dependency graph are not allowed, as it would lead to endless runs of observers
            // and would prevent the system from finding a valid order to call the observers.
            // As a special case, however, we allow an observer to observe a component and modify it.
            // In that case, the observer is moved to the end of the observer call list.
            for (var index = observersToSort.Count - 1; index >= 0; index--)
            {
                var observer = observersToSort[index];
                if (observer.ObservedStateComponents.Any(observedStateComponent => observer.ModifiedStateComponents.Contains(observedStateComponent)))
                {
                    observersToSort.SwapRemoveAt(index);
                    endBatch.Add(observer);
                }
            }

            var cycleDetected = false;
            cycleDetected |= SortObservers(observersToSort, sortedObservers, 0);
            cycleDetected |= SortObservers(endBatch, sortedObservers, 1);

            if (cycleDetected)
            {
                Debug.LogWarning("Dependency cycle detected in observers.");
            }
        }

        // Will modify observersToSort.
        // Returns true if a dependency cycle was detected.
        static bool SortObservers(List<IStateObserver> observersToSort, List<IStateObserver> sortedObservers, int conditionCount)
        {
            if (observersToSort.Count == 0)
                return false;

            var modifiedStates = new List<IStateComponent>();
            foreach (var observer in observersToSort)
            {
                modifiedStates.AddRange(observer.ModifiedStateComponents);
            }

            var cycleDetected = false;
            while (observersToSort.Count > 0 && !cycleDetected)
            {
                var remainingObserverCount = observersToSort.Count;
                for (var index = observersToSort.Count - 1; index >= 0; index--)
                {
                    var observer = observersToSort[index];

                    // conditionCount is the number of times an observedStateComponent is allowed to appear in the
                    // modifiedStates list in order to be picked up and added to the sortedObservers list.
                    // In general, it is zero (because modifications should be done before observations),
                    // but it can be one if we allow observers to modify a state component it observes.
                    if (observer.ObservedStateComponents.Any(observedStateComponent => ContainsMoreThan(modifiedStates, observedStateComponent, conditionCount)))
                    {
                        remainingObserverCount--;
                    }
                    else
                    {
                        foreach (var modifiedStateComponent in observer.ModifiedStateComponents)
                        {
                            modifiedStates.SwapRemove(modifiedStateComponent);
                        }

                        // Not using SwapRemoveAt to keep observersToSort in the same order for the duration of the while loop.
                        observersToSort.RemoveAt(index);
                        sortedObservers.Add(observer);
                    }
                }

                cycleDetected = remainingObserverCount == 0;
            }

            if (observersToSort.Count > 0)
            {
                // The remaining observers could not be sorted because of a dependency cycle.
                // We prefer to execute them in the wrong order rather than not executing them at all.
                sortedObservers.AddRange(observersToSort);
            }

            return cycleDetected;
        }

        /// <summary>
        /// Notifies state observers that the state they observe has changed.
        /// </summary>
        /// <param name="state">The state.</param>
        public void NotifyObservers(IState state)
        {
            if (!IsObserving)
            {
                try
                {
                    IsObserving = true;

                    if (m_SortedObservers == null)
                        SortObservers();

                    m_ObserverCallList.Clear();
                    if (m_SortedObservers !.Count > 0)
                    {
                        m_DirtyComponentSet.Clear();

                        foreach (var observer in m_SortedObservers)
                        {
                            var addToCallList =
                                m_DirtyComponentSet.Overlaps(observer.ObservedStateComponents) ||
                                m_NewObservers.Contains(observer);

                            if (!addToCallList)
                            {
                                foreach (var observedStateComponent in observer.ObservedStateComponents)
                                {
                                    var lastObservedVersion = observer.GetLastObservedComponentVersion(observedStateComponent);
                                    var updateType = observedStateComponent.GetObserverUpdateType(lastObservedVersion);
                                    if (updateType != UpdateType.None)
                                    {
                                        addToCallList = true;
                                        break;
                                    }
                                }
                            }

                            if (addToCallList)
                            {
                                m_ObserverCallList.Add(observer);
                                m_DirtyComponentSet.UnionWith(observer.ModifiedStateComponents);
                            }
                        }

                        m_NewObservers.Clear();
                    }

                    if (m_ObserverCallList.Count > 0)
                    {
                        try
                        {
                            foreach (var observer in m_ObserverCallList)
                            {
                                state.CurrentObserver = observer;
                                using (m_ProfilerMarkers[observer].Auto())
                                {
                                    observer.Observe();
                                }
                            }
                        }
                        finally
                        {
                            state.CurrentObserver = null;
                        }

                        // If m_ObserverCallList is empty, observed versions did not change, so changesets do not need to be purged.

                        // For each state component, find the earliest observed version in all observers and purge the
                        // changesets that are earlier than this earliest version.
                        foreach (var editorStateComponent in state.AllStateComponents)
                        {
                            var stateComponentHashCode = editorStateComponent.GetHashCode();

                            var earliestObservedVersion = uint.MaxValue;

                            if (m_StateObservers.TryGetValue(editorStateComponent, out var observersForComponent))
                            {
                                // Not using List.Min to avoid closure allocation.
                                foreach (var observer in observersForComponent)
                                {
                                    var v = observer.GetLastObservedComponentVersion(editorStateComponent);
                                    var versionNumber = v.HashCode == stateComponentHashCode ? v.Version : uint.MinValue;
                                    earliestObservedVersion = Math.Min(earliestObservedVersion, versionNumber);
                                }
                            }

                            editorStateComponent.ChangesetManager?.RemoveObsoleteChangesets(earliestObservedVersion, editorStateComponent.CurrentVersion);
                        }
                    }
                }
                finally
                {
                    m_ObserverCallList.Clear();
                    m_DirtyComponentSet.Clear();
                    IsObserving = false;
                }
            }
        }
    }
}
