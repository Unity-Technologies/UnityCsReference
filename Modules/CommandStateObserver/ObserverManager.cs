// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// A class responsible for notifying observers when the state they observe is dirtied.
    /// </summary>
    class ObserverManager
    {
        /// <summary>
        /// A mapping of state component to observers observing those components.
        /// </summary>
        protected readonly Dictionary<IStateComponent, List<IStateObserver>> m_StateObservers = new Dictionary<IStateComponent, List<IStateObserver>>();

        List<IStateObserver> m_SortedObservers = new List<IStateObserver>();
        readonly List<IStateObserver> m_ObserverCallList = new List<IStateObserver>();
        readonly HashSet<IStateComponent> m_DirtyComponentSet = new HashSet<IStateComponent>();
        readonly List<IStateObserver> m_NewObservers = new List<IStateObserver>();

        /// <summary>
        /// Returns true if the observers are being notified.
        /// </summary>
        bool IsObserving { get; set; }

        /// <summary>
        /// Registers a state observer.
        /// </summary>
        /// <remarks>
        /// The content of <see cref="StateObserver.ObservedStateComponents"/> and
        /// <see cref="StateObserver.ModifiedStateComponents"/> should not change once the
        /// observer is registered.
        /// </remarks>
        /// <param name="observer">The observer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the observer is already registered.</exception>
        public void RegisterObserver(IStateObserver observer)
        {
            if (observer == null)
                return;

            m_NewObservers.Add(observer);

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
        /// Unregisters a state observer.
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
        }

        void SortObservers()
        {
            var observers = m_StateObservers.Values.SelectMany(x => x)
                .Distinct()
                .ToList();

            SortObservers_Internal(observers, out m_SortedObservers);
        }

        // Will modify observersToSort.
        internal static void SortObservers_Internal(List<IStateObserver> observersToSort, out List<IStateObserver> sortedObservers)
        {
            sortedObservers = new List<IStateObserver>(observersToSort.Count);
            var modifiedStates = observersToSort.SelectMany(observer => observer.ModifiedStateComponents).ToList();

            var cycleDetected = false;
            while (observersToSort.Count > 0 && !cycleDetected)
            {
                var remainingObserverCount = observersToSort.Count;
                for (var index = observersToSort.Count - 1; index >= 0; index--)
                {
                    var observer = observersToSort[index];

                    if (observer.ObservedStateComponents.Any(observedStateComponent => modifiedStates.Contains(observedStateComponent)))
                    {
                        remainingObserverCount--;
                    }
                    else
                    {
                        foreach (var modifiedStateComponent in observer.ModifiedStateComponents)
                        {
                            modifiedStates.Remove(modifiedStateComponent);
                        }

                        observersToSort.RemoveAt(index);
                        sortedObservers.Add(observer);
                    }
                }

                cycleDetected = remainingObserverCount == 0;
            }

            if (observersToSort.Count > 0)
            {
                Debug.LogWarning("Dependency cycle detected in observers.");
                sortedObservers.AddRange(observersToSort);
            }
        }

        /// <summary>
        /// Notifies state observers that the state has changed.
        /// </summary>
        /// <remarks>
        /// State observers will only be notified if the state components they are observing have changed.
        /// </remarks>
        /// <param name="state">The observed state.</param>
        public virtual void NotifyObservers(IState state)
        {
            if (!IsObserving)
            {
                try
                {
                    IsObserving = true;

                    if (m_SortedObservers == null)
                        SortObservers();

                    m_ObserverCallList.Clear();
                    if (m_SortedObservers.Count > 0)
                    {
                        m_DirtyComponentSet.Clear();

                        foreach (var component in state.AllStateComponents)
                        {
                            if (component.HasChanges())
                            {
                                m_DirtyComponentSet.Add(component);
                            }
                        }

                        if (m_DirtyComponentSet.Count > 0 || m_NewObservers.Count > 0)
                        {
                            foreach (var observer in m_SortedObservers)
                            {
                                if (m_DirtyComponentSet.Overlaps(observer.ObservedStateComponents) || m_NewObservers.Contains(observer))
                                {
                                    m_ObserverCallList.Add(observer);
                                    m_DirtyComponentSet.UnionWith(observer.ModifiedStateComponents);
                                }
                            }

                            m_NewObservers.Clear();
                        }
                    }

                    if (m_ObserverCallList.Any())
                    {
                        try
                        {
                            foreach (var observer in m_ObserverCallList)
                            {
                                StateObserverHelper_Internal.CurrentObserver_Internal = observer;
                                observer.Observe();
                            }
                        }
                        finally
                        {
                            StateObserverHelper_Internal.CurrentObserver_Internal = null;
                        }

                        // If m_ObserverCallSet is empty, observed versions did not change, so changesets do not need to be purged.

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
                                    var v = (observer as IInternalStateObserver_Internal)?.GetLastObservedComponentVersion_Internal(editorStateComponent) ?? default;
                                    var versionNumber = v.HashCode == stateComponentHashCode ? v.Version : uint.MinValue;
                                    earliestObservedVersion = Math.Min(earliestObservedVersion, versionNumber);
                                }
                            }

                            editorStateComponent.PurgeOldChangesets(earliestObservedVersion);
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
