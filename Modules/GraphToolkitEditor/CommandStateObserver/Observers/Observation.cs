// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;
using UnityEngine.Assertions;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// A class used to represent observations by an <see cref="IStateObserver"/> on an <see cref="IStateComponent"/>.
    /// </summary>
    /// <remarks>
    /// Instances of this class can be used in a using() context. To get an <see cref="Observation"/> object,
    /// use one of the <see cref="StateObserverExtensions"/> method.
    /// </remarks>
    [UnityRestricted]
    internal class Observation : IDisposable
    {
        IStateComponent m_ObservedComponent;
        IStateObserver m_Observer;
        bool m_UpdateObserverVersion;

        /// <summary>
        /// The update type for the observation.
        /// </summary>
        public UpdateType UpdateType { get; }

        /// <summary>
        /// The observer's last observed version of the component.
        /// </summary>
        public uint LastObservedVersion { get; }

        internal static Observation Create(IStateObserver observer, IStateComponent observedComponent, bool updateObserverVersion = true)
        {
            return new Observation(observer, observedComponent, updateObserverVersion);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Observation" /> class.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="observedComponent">The observed state</param>
        /// <param name="updateObserverVersion">True if we should update the observer's observed version
        /// at the end of the observation (default). False otherwise.</param>
        Observation(IStateObserver observer, IStateComponent observedComponent, bool updateObserverVersion = true)
        {

            m_ObservedComponent = observedComponent;
            m_Observer = observer;
            m_UpdateObserverVersion = updateObserverVersion;

            var lastObservedVersion = m_Observer.GetLastObservedComponentVersion(observedComponent);
            UpdateType = m_ObservedComponent.GetObserverUpdateType(lastObservedVersion);
            LastObservedVersion = lastObservedVersion.Version;
        }

        ~Observation()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_UpdateObserverVersion)
                {
                    var newVersion = new StateComponentVersion(m_ObservedComponent.GetHashCode(), m_ObservedComponent.CurrentVersion);
                    m_Observer.UpdateObservedVersion(m_ObservedComponent, newVersion);
                }
            }
        }
    }
}
