// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Base class for implementations of <see cref="IUndoableStateComponent"/>.
    /// </summary>
    /// <remarks>
    /// <p>
    /// <see cref="StateComponent{TUpdater}"/> should expose a readonly interface to the data they hold.
    /// All updates to a <see cref="StateComponent{TUpdater}"/>
    /// should be done through an updater, which is an instance of a class deriving from <see cref="BaseUpdater{T}"/>.
    /// This ensures that change tracking is done properly.
    /// </p>
    /// <p>
    /// <see cref="StateComponent{TUpdater}"/>s are serialized and deserialized on undo/redo.
    /// Make sure that their fields behave properly
    /// under these conditions. Either put the <see cref="SerializeField"/> attribute on them or check that
    /// they are initialized before accessing them.
    /// </p>
    /// </remarks>
    [Serializable]
    abstract class StateComponent<TUpdater> : IUndoableStateComponent, ISerializationCallbackReceiver
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


            /// <summary>
            /// Initializes the updater with the state to update.
            /// </summary>
            /// <param name="state">The state to update.</param>
            public void Initialize(IStateComponent state)
            {
                if (m_State != null)
                {
                    Debug.LogError("Missing Dispose call. Did you nest update scopes for the same state component?");
                }

                m_State = (TStateComponent)state;

                if (StateObserverHelper_Internal.CurrentObserver_Internal != null &&
                    !StateObserverHelper_Internal.CurrentObserver_Internal.ModifiedStateComponents.Contains(m_State))
                {
                    Debug.LogError($"Observer {StateObserverHelper_Internal.CurrentObserver_Internal?.GetType()} does not specify that it modifies {m_State}. Please add the state component to its {nameof(IStateObserver.ModifiedStateComponents)}.");
                }

                m_State.BeginChangeScope();
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
                    m_State.EndChangeScope();
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

            /// <summary>
            /// Force the state component to tell its observers to do a complete update.
            /// </summary>
            public void ForceCompleteUpdate()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Moves the content of a state component loaded from persistent storage into this state component.
            /// </summary>
            /// <param name="other">The source state component.</param>
            /// <remarks>The <paramref name="other"/> state components will be discarded after the call to
            /// <see cref="RestoreFromPersistedState"/>.
            /// This means you do not need to make a deep copy of the data: just copying the references is sufficient.
            /// </remarks>
            public void RestoreFromPersistedState(IStateComponent other)
            {
                m_State.Move(other, null);
                // m_State.Move() is responsible for calling SetUpdateType(something).
            }

            /// <summary>
            /// Moves the content of a state component obtained from the undo stack into this state component.
            /// </summary>
            /// <param name="other">The source state component.</param>
            /// <param name="changeset">A description of what changed between this and <paramref name="other"/>.</param>
            /// <remarks>The <paramref name="other"/> state components will be discarded after the call to
            /// <see cref="RestoreFromUndo"/>.
            /// This means you do not need to make a deep copy of the data: just copying the references is sufficient.
            /// </remarks>
            public void RestoreFromUndo(IStateComponent other, IChangeset changeset)
            {
                m_State.Move(other, changeset);
                // m_State.Move() is responsible for calling SetUpdateType(something).
            }
        }

        [SerializeField]
        Hash128 m_Guid;

        [SerializeField]
        uint m_Version;

        UpdateType m_ScopeUpdateType = UpdateType.None;

        TUpdater m_Updater;

        /// <summary>
        /// A unique id for the state component.
        /// </summary>
        public Hash128 Guid => m_Guid;

        /// <summary>
        /// The state from which this state component is part of.
        /// </summary>
        public IState State { get; private set; }

        /// <summary>
        /// The changeset manager, if any changesets are recorded.
        /// </summary>
        public virtual ChangesetManager ChangesetManager => null;

        /// <summary>
        /// The state component name.
        /// </summary>
        public virtual string ComponentName => GetType().FullName;

        /// <summary>
        /// The current version of the state component.
        /// </summary>
        public uint CurrentVersion { get; private set; } = 1;

        /// <summary>
        /// The updater for the state component.
        /// </summary>
        /// <remarks>Since state component expose a read only interface, all modifications
        /// to a state component need to be done through this Updater.</remarks>
        public TUpdater UpdateScope
        {
            get
            {
                m_Updater ??= new TUpdater();

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
        /// Prepares the <see cref="StateComponent{TUpdater}"/> to accept changes.
        /// </summary>
        protected virtual void BeginChangeScope()
        {
            m_ScopeUpdateType = UpdateType.None;
        }

        /// <summary>
        /// Does housekeeping after changes are done on the <see cref="StateComponent{TUpdater}"/>.
        /// </summary>
        protected virtual void EndChangeScope()
        {
            if (m_ScopeUpdateType != UpdateType.None)
            {
                CurrentVersion++;

                if (ChangesetManager != null)
                {
                    // If update type is Complete, there is no need to push the changeset, as they cannot be used for an update.
                    if (m_ScopeUpdateType != UpdateType.Complete)
                    {
                        ChangesetManager.PushCurrentChangeset(CurrentVersion);
                    }
                    else
                    {
                        ChangesetManager.PushNullChangeset(CurrentVersion);
                    }
                }
            }
        }

        /// <summary>
        /// Purges the changesets that track changes up to and including <paramref name="upToAndIncludingVersion"/>.
        /// </summary>
        /// <remarks>
        /// The state component can choose to purge more recent changesets.
        /// </remarks>
        /// <param name="upToAndIncludingVersion">Version up to which we should purge changesets. Pass uint.MaxValue to purge all changesets.</param>
        public virtual void PurgeObsoleteChangesets(uint upToAndIncludingVersion)
        {
            ChangesetManager?.RemoveObsoleteChangesets(upToAndIncludingVersion, CurrentVersion);
        }

        /// <summary>
        /// Sets the kind of update that was done on the state component.
        /// If the update type is already set, it will be changed only
        /// if the new update type has a higher value than the current one.
        /// </summary>
        /// <param name="type">The update type.</param>
        // Internal for tests only.
        protected internal void SetUpdateType(UpdateType type)
        {
            if (type > m_ScopeUpdateType)
                m_ScopeUpdateType = type;
        }

        // For tests only.
        internal void ForceSetUpdateType_Internal(UpdateType type)
        {
            m_ScopeUpdateType = type;
        }

        /// <summary>
        /// Gets the type of update an observer should do.
        /// </summary>
        /// <param name="observerVersion">The last state component version observed by the observer.</param>
        /// <returns>Returns the type of update an observer should do.</returns>
        public UpdateType GetObserverUpdateType(StateComponentVersion observerVersion)
        {
            // Version does not match the state component.
            if (observerVersion.HashCode != GetHashCode())
            {
                return UpdateType.Complete;
            }

            // If the observer is new, force a complete update.
            if (observerVersion.Version == 0)
            {
                return UpdateType.Complete;
            }

            if (observerVersion.Version == CurrentVersion)
            {
                return UpdateType.None;
            }

            if (ChangesetManager == null)
            {
                return UpdateType.Complete;
            }

            // If ChangesetManager has changesets to go from observerVersion.Version to CurrentVersion,
            // do a partial update. Otherwise do a complete update.
            var hasValidChangesetForVersions = ChangesetManager.HasValidChangesetForVersions(observerVersion.Version, CurrentVersion);
            return hasValidChangesetForVersions ? UpdateType.Partial : UpdateType.Complete;
        }

        /// <summary>
        /// Called when the state component has been added to the state.
        /// </summary>
        /// <param name="state">The state to which the state component was added.</param>
        public virtual void OnAddedToState(IState state)
        {
            State = state;
        }

        /// <summary>
        /// Called when the state component has been removed from the state.
        /// </summary>
        /// <param name="state">The state from which the state component was removed.</param>
        public virtual void OnRemovedFromState(IState state)
        {
            State = null;
            ChangesetManager?.RemoveAllChangesets();
        }

        /// <summary>
        /// Moves the content of another state component into this state component.
        /// </summary>
        /// <param name="other">The source state component.</param>
        /// <param name="changeset">Details about what changed between <paramref name="other"/>
        /// state (considered the ancestor) and the current state.</param>
        /// <remarks>
        /// <p>
        /// This should only be called from <see cref="IStateComponentUpdater.RestoreFromPersistedState"/>
        /// or <see cref="IStateComponentUpdater.RestoreFromUndo"/>.
        /// </p><p>
        /// Overrides should call <see cref="SetUpdateType"/> as necessary, since the state updater will not call it.
        /// </p><p>
        /// The <paramref name="other"/> state component will be discarded after the call to Move.
        /// This means you do not need to make a deep copy of the data: just copying the references is sufficient.
        /// </p>
        /// </remarks>
        protected virtual void Move(IStateComponent other, IChangeset changeset)
        {
            if (other is StateComponent<TUpdater> stateComponent)
            {
                m_Guid = stateComponent.Guid;
            }
        }

        /// <summary>
        /// Called before the state component is pushed on the undo stack.
        /// Use this to push additional objects on the stack.
        /// </summary>
        /// <param name="undoString">The name of the undo operation.</param>
        public virtual void WillPushOnUndoStack(string undoString)
        {
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            m_Version = CurrentVersion;
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            CurrentVersion = m_Version;
        }

        /// <summary>
        /// Replaces serialized values of this component by values from <paramref name="undoData"/>.
        /// </summary>
        /// <param name="undoData">The state component from which to take the values.</param>
        /// <param name="changeset">A description of the changes brought in by <paramref name="undoData"/>.
        /// If null, anything may have change.</param>
        public virtual void ApplyUndoData(IStateComponent undoData, IChangeset changeset)
        {
            if (undoData != null)
            {
                using (var updater = UpdateScope)
                {
                    updater.RestoreFromUndo(undoData, changeset);
                }
            }
        }

        /// <summary>
        /// Called after an undo/redo operation, when the state component can be affected by the operation.
        /// </summary>
        /// <param name="isRedo">True if the operation is a redo, false if the operation is an undo.</param>
        public virtual void UndoRedoPerformed(bool isRedo)
        {
        }
    }
}
