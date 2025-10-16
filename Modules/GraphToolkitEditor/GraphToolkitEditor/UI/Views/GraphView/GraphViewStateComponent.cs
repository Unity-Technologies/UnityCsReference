// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A component to hold the editor state of the <see cref="GraphView"/> for a graph object.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class GraphViewStateComponent : PersistedStateComponent<GraphViewStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for the <see cref="GraphViewStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<GraphViewStateComponent>, IOnGraphLoaded
        {
            /// <summary>
            /// Saves the current state and loads the state associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph for which to load the state component.</param>
            public void OnGraphLoaded(GraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }

            /// <summary>
            /// The scale factor of the <see cref="GraphView"/>.
            /// </summary>
            public Vector3 Scale
            {
                set
                {
                    if (m_State.m_Scale != value)
                    {
                        m_State.m_Scale = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// The position of the <see cref="GraphView"/>.
            /// </summary>
            public Vector3 Position
            {
                set
                {
                    if (m_State.m_Position != value)
                    {
                        m_State.m_Position = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// Sets the index of the current error; used by the error toolbar.
            /// </summary>
            /// <param name="index">The index of the error.</param>
            public void SetCurrentErrorIndex(int index)
            {
                m_State.ErrorIndex = index;
            }

            /// <summary>
            /// Marks the state component as changed. Used to communicate UI-only changes
            /// that occurs in <see cref="SelectionDragger"/> and <see cref="ContentDragger"/>
            /// to the <see cref="MiniMap"/>.
            /// </summary>
            internal void MarkContentUpdated()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        internal static Vector3 defaultScale = Vector3.one;
        internal static Vector3 defaultPosition = Vector3.zero;

        [SerializeField]
        Vector3 m_Scale = defaultScale;

        [SerializeField]
        Vector3 m_Position = defaultPosition;

        /// <summary>
        /// The scale (zoom factor) of the graph view.
        /// </summary>
        public Vector3 Scale => m_Scale;

        /// <summary>
        /// The position of the graph view.
        /// </summary>
        public Vector3 Position => m_Position;

        /// <summary>
        /// The index of the current error.
        /// </summary>
        public int ErrorIndex { get; private set; }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is GraphViewStateComponent graphViewStateComponent)
            {
                SetUpdateType(UpdateType.Complete);

                m_Scale = graphViewStateComponent.m_Scale;
                m_Position = graphViewStateComponent.m_Position;
                ErrorIndex = graphViewStateComponent.ErrorIndex;
            }
        }
    }
}
