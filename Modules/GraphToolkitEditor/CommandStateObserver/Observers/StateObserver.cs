// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Base class for state observers.
    /// </summary>
    [UnityRestricted]
    internal abstract class StateObserver : IStateObserver
    {
        List<IStateComponent> m_ObservedStateComponents;
        List<StateComponentVersion> m_ObservedComponentVersions;
        List<IStateComponent> m_ModifiedStateComponents;

        /// <summary>
        /// The state components observed by the observer.
        /// </summary>
        public IReadOnlyList<IStateComponent> ObservedStateComponents => m_ObservedStateComponents;

        /// <summary>
        /// The state components that can be modified by the observer.
        /// </summary>
        public IReadOnlyList<IStateComponent> ModifiedStateComponents => m_ModifiedStateComponents;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateObserver" /> class.
        /// </summary>
        /// <param name="observedStateComponents">The names of the observed state components.</param>
        protected StateObserver(params IStateComponent[] observedStateComponents)
            : this(observedStateComponents, Array.Empty<IStateComponent>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateObserver" /> class.
        /// </summary>
        /// <param name="observedStateComponents">The names of the observed state components.</param>
        /// <param name="modifiedStateComponents">The names of the modified state components.</param>
        protected StateObserver(IStateComponent[] observedStateComponents, IStateComponent[] modifiedStateComponents)
        {
            m_ObservedStateComponents = new List<IStateComponent>();
            m_ObservedStateComponents.AddFewDistinct(observedStateComponents);

            m_ObservedComponentVersions = new List<StateComponentVersion>(m_ObservedStateComponents.Count);
            for (int i = 0; i < m_ObservedStateComponents.Count; i++)
            {
                m_ObservedComponentVersions.Add(default);
            }

            m_ModifiedStateComponents = new List<IStateComponent>();
            m_ModifiedStateComponents.AddFewDistinct(modifiedStateComponents);
        }

        /// <summary>
        /// Gets the last observed component version of <paramref name="stateComponent"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to get the last observed version.</param>
        /// <returns>Returns the last observed component version of <paramref name="stateComponent"/>.</returns>
        public StateComponentVersion GetLastObservedComponentVersion(IStateComponent stateComponent)
        {
            var index = m_ObservedStateComponents.IndexOf(stateComponent);
            return index >= 0 ? m_ObservedComponentVersions[index] : default;
        }

        /// <summary>
        /// Updates the observed version for component <paramref name="stateComponent"/> to <paramref name="newVersion"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to update the version.</param>
        /// <param name="newVersion">The new version.</param>
        public void UpdateObservedVersion(IStateComponent stateComponent, StateComponentVersion newVersion)
        {
            var index = m_ObservedStateComponents.IndexOf(stateComponent);
            if (index >= 0)
                m_ObservedComponentVersions[index] = newVersion;
        }

        /// <summary>
        /// Observes the <see cref="IStateObserver.ObservedStateComponents"/> and modifies the <see cref="IStateObserver.ModifiedStateComponents"/>.
        /// </summary>
        public abstract void Observe();
    }
}
