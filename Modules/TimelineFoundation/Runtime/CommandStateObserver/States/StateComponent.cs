// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using Unity.Timeline.Foundation.CSO.Internals;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Unity.Timeline.Foundation.CSO
{
    /// <summary>
    /// Base class for implementations of <see cref="IStateComponent"/>.
    /// </summary>
    /// <remarks>
    /// <p>
    /// State components should expose a readonly interface. All updates to a state component
    /// should be done through an updater, which is an instance of a class deriving from <see cref="BaseUpdater{T}"/>.
    /// This ensures that change tracking is done properly.
    /// </p>
    /// <p>
    /// State components are serialized and deserialized on undo/redo. Make sure that their fields behave properly
    /// under these conditions. Either put the Serialized attribute on them or check that they are initialized
    /// before accessing them.
    /// </p>
    /// </remarks>
    [Serializable]
    abstract class StateComponent<TUpdater> : IStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        /// <summary>
        /// Updater class for the state component.
        /// </summary>
        /// <typeparam name="TStateComponent">The type of state component that is to be updated.</typeparam>
        public abstract class BaseUpdater<TStateComponent> : IStateComponentUpdater where TStateComponent : StateComponent<TUpdater>
        {
            /// <summary>
            /// The state component that can be updated through this updater.
            /// </summary>
            protected TStateComponent m_State;


            /// <inheritdoc />
            public void Initialize(IStateComponent state)
            {
                if (m_State != null)
                {
                    Debug.LogError("Missing Dispose call. Did you nest update scopes for the same state component?");
                }

                m_State = state as TStateComponent;
                BeginStateChange();
            }

            ~BaseUpdater()
            {
                Dispose(false);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose implementation.
            /// </summary>
            /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
            /// Otherwise it is called from the finalizer.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Assert.IsNotNull(m_State, "Missing Initialize call.");
                    EndStateChange();
                    m_State = null;
                }
            }

            /// <summary>
            /// Whether this updater is associated with the stateComponent.
            /// </summary>
            /// <param name="stateComponent">The state component.</param>
            /// <returns>True if this updater is associated with the stateComponent, false otherwise.</returns>
            public bool IsUpdaterForState(IStateComponent stateComponent)
            {
                return m_State == stateComponent;
            }

            void BeginStateChange()
            {
#pragma warning disable UA2007 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (StateObserverHelper.CurrentObserver != null &&
                    !StateObserverHelper.CurrentObserver.ModifiedStateComponents.Contains(m_State))
#pragma warning restore UA2007
                {
                    Debug.LogError($"Observer {StateObserverHelper.CurrentObserver?.GetType()} does not specify that it modifies {m_State}. Please add the state component to its {nameof(IStateObserver.ModifiedStateComponents)}.");
                }

                m_State.PushChangeset(m_State.CurrentVersion);
            }

            void EndStateChange()
            {
                // unchecked: wrap around on overflow without exception.
                unchecked
                {
                    m_State.CurrentVersion++;
                }
            }

            /// <summary>
            /// Force the state component to ask its observers to do a complete update.
            /// </summary>
            public void ForceCompleteUpdate()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Moves data from <paramref name="other"/> into this state component. After the operation,
            /// <paramref name="other"/> must be in a valid state but does not have to contain its original data.
            /// </summary>
            /// <param name="other">The state component from which data is moved.</param>
            public void Move(IStateComponent other)
            {
                m_State.Move(other);
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        [SerializeField]
        Hash128 m_Guid;

        /// <inheritdoc />
        public Hash128 Guid => m_Guid;

        UpdateType m_UpdateType = UpdateType.None;

        TUpdater m_Updater;

        /// <summary>
        /// The state from which this state component is part of.
        /// </summary>
        public IState State { get; private set; }

        /// <summary>
        /// The changeset manager, if any changesets are recorded.
        /// </summary>
        protected virtual IChangesetManager ChangesetManager => null;

        /// <summary>
        /// The state component name.
        /// </summary>
        public virtual string ComponentName => GetType().FullName;

        /// <summary>
        /// The earliest changeset version held by this state component.
        /// </summary>
        protected uint EarliestChangeSetVersion { get; set; }

        /// <inheritdoc />
        public uint CurrentVersion { get; private set; } = 1;

        /// <summary>
        /// The updater for the state component.
        /// </summary>
        /// <remarks>Since state component expose a read only interfaces, all modifications
        /// to a state component need to be done through this Updater.</remarks>
        public TUpdater UpdateScope
        {
            get
            {
                if (m_Updater == null)
                    m_Updater = new TUpdater();

                m_Updater.Initialize(this);
                return m_Updater;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateComponent{TUpdater}"/> class.
        /// </summary>
        protected StateComponent()
        {
            m_Guid = new Hash128();
            m_Guid.Append(Random.value);
        }

        /// <summary>
        /// Push the current changeset and tag it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number associated with the changeset.</param>
        protected virtual void PushChangeset(uint version)
        {
            // If update type is Complete, there is no need to push the changeset, as they cannot be used for an update.
            if (m_UpdateType != UpdateType.Complete)
                ChangesetManager?.PushChangeset(version);
        }

        /// <inheritdoc />
        public virtual void PurgeOldChangesets(uint untilVersion)
        {
            // StateComponent default implementation does not record changesets,
            // so m_EarliestChangeSetVersion is set to the CurrentVersion.
            EarliestChangeSetVersion = ChangesetManager?.PurgeOldChangesets(untilVersion, CurrentVersion) ?? CurrentVersion;
            ResetUpdateType();
        }

        /// <inheritdoc/>
        public bool HasChanges()
        {
            return EarliestChangeSetVersion != CurrentVersion;
        }

        /// <summary>
        /// Clears the update type if there are no pending changes.
        /// </summary>
        protected void ResetUpdateType()
        {
            if (EarliestChangeSetVersion == CurrentVersion)
                m_UpdateType = UpdateType.None;
        }

        /// <summary>
        /// Set how the observers should update themselves. Unless <paramref name="force"/> is true,
        /// if the update type is already set, it will be changed only
        /// if the new update type has a higher value than the current one.
        /// </summary>
        /// <param name="type">The update type.</param>
        /// <param name="force">Set the update type even if the new value is lower than the current one.</param>
        public virtual void SetUpdateType(UpdateType type, bool force = false)
        {
            if (type > m_UpdateType || force)
                m_UpdateType = type;

            // If update type is Complete, there is no need to keep the changesets, as they cannot be used for an update.
            if (m_UpdateType == UpdateType.Complete)
            {
                ChangesetManager?.PurgeOldChangesets(CurrentVersion, CurrentVersion);
            }
        }

        /// <inheritdoc/>
        public UpdateType GetUpdateType(StateComponentVersion observerVersion)
        {
            if (observerVersion.HashCode != GetHashCode())
            {
                return UpdateType.Complete;
            }

            // If view is new or too old, tell it to rebuild itself completely.
            if (observerVersion.Version == 0 || observerVersion.Version < EarliestChangeSetVersion)
            {
                return UpdateType.Complete;
            }

            // This is safe even if Version wraps around after an overflow.
            return observerVersion.Version == CurrentVersion ? UpdateType.None : m_UpdateType;
        }

        /// <inheritdoc />
        public virtual void OnAddedToState(IState state)
        {
            State = state;
        }

        /// <inheritdoc />
        public virtual void OnRemovedFromState(IState state)
        {
            State = null;
            ChangesetManager?.PurgeOldChangesets(uint.MaxValue, CurrentVersion);
        }

        /// <inheritdoc  cref="IStateComponentUpdater.Move"/>
        protected virtual void Move(IStateComponent other)
        {
            if (other is StateComponent<TUpdater> stateComponent)
            {
                m_Guid = stateComponent.Guid;
            }
        }
    }
}
