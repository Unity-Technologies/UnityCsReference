// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for state components that store data associated with an graph and/or a view.
    /// </summary>
    interface IPersistedStateComponent : IStateComponent
    {
        /// <summary>
        /// The unique ID of the associated view. Set to `default` if the state component is not associated with any view.
        /// </summary>
        Hash128 ViewGuid { get; set; }

        /// <summary>
        /// A unique key for the graph associated with this state component. Set to `default` if the state component is not associated with any graph.
        /// </summary>
        string GraphKey { get; set; }
    }
}
