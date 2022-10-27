// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for implementations of <see cref="IPersistedStateComponent"/>.
    /// </summary>
    [Serializable]
    abstract class PersistedStateComponent<TUpdater> : StateComponent<TUpdater>, IPersistedStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        [SerializeField]
        Hash128 m_ViewGuid;

        [FormerlySerializedAs("m_AssetKey")]
        [SerializeField]
        string m_GraphKey;

        /// <inheritdoc/>
        public Hash128 ViewGuid
        {
            get => m_ViewGuid;
            set => m_ViewGuid = value;
        }

        /// <inheritdoc />
        public string GraphKey
        {
            get => m_GraphKey;
            set => m_GraphKey = value;
        }

        /// <inheritdoc />
        public override void OnRemovedFromState(IState state)
        {
            base.OnRemovedFromState(state);
            PersistedState.StoreStateComponent(this, ComponentName, ViewGuid, GraphKey);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is PersistedStateComponent<TUpdater> persistedStateComponent)
            {
                m_ViewGuid = persistedStateComponent.ViewGuid;
                m_GraphKey = persistedStateComponent.GraphKey;
            }
        }
    }
}
