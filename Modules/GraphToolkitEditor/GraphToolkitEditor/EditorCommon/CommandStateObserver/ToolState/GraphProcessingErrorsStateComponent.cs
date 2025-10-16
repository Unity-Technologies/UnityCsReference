// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Holds a list of <see cref="GraphProcessingErrorModel"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class GraphProcessingErrorsStateComponent : StateComponent<GraphProcessingErrorsStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for <see cref="GraphProcessingErrorsStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<GraphProcessingErrorsStateComponent>, IOnGraphLoaded
        {
            /// <summary>
            /// Sets the error list.
            /// </summary>
            /// <param name="errorModels">The errors.</param>
            public void SetResults(IEnumerable<MultipleGraphProcessingErrorsModel> errorModels)
            {
                m_State.m_Errors.Clear();

                if (errorModels != null)
                    m_State.m_Errors.AddRange(errorModels);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Clears the error list.
            /// </summary>
            public void OnGraphLoaded(GraphModel graphModel)
            {
                m_State.m_Errors.Clear();
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        List<MultipleGraphProcessingErrorsModel> m_Errors = new();

        /// <summary>
        /// The errors.
        /// </summary>
        public IReadOnlyList<MultipleGraphProcessingErrorsModel> Errors => m_Errors;

        /// <inheritdoc/>
        public override void OnRemovedFromState(IState state)
        {
            base.OnRemovedFromState(state);
            m_Errors.Clear();
        }
    }
}
