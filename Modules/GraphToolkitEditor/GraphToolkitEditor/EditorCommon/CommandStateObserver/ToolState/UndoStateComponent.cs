// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A state component to store undo information.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal sealed class UndoStateComponent : StateComponent<UndoStateComponent.StateUpdater>, IUndoableCommandMerger
    {
        /// <summary>
        /// Updater for <see cref="UndoStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<UndoStateComponent>
        {
            /// <summary>
            /// Save a state component on the undo stack.
            /// </summary>
            /// <param name="stateComponent">The state component to save.</param>
            public void SaveState(IUndoableStateComponent stateComponent)
            {
                m_State.m_UndoStateRecorder.RecordComponent(stateComponent);
            }

            /// <summary>
            /// Save state components on the undo stack.
            /// </summary>
            /// <param name="stateComponents">The state components to save.</param>
            public void SaveStates(IEnumerable<IUndoableStateComponent> stateComponents)
            {
                m_State.m_UndoStateRecorder.RecordComponents(stateComponents);
            }

            /// <summary>
            /// Save state components on the undo stack.
            /// </summary>
            /// <param name="stateComponents">The state components to save.</param>
            public void SaveStates(params IUndoableStateComponent[] stateComponents)
            {
                m_State.m_UndoStateRecorder.RecordComponents(stateComponents);
            }
        }

        IState m_State;
        UndoStateRecorder m_UndoStateRecorder;

        [NonSerialized]
        ToolStateComponent m_ToolStateComponent;

        public bool IsMerging => m_UndoStateRecorder.IsMergingCommands;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoStateComponent"/> class.
        /// </summary>
        public UndoStateComponent(IState state, ToolStateComponent toolStateComponent, UndoStateRecorder stateRecorder)
        {
            m_State = state;
            m_UndoStateRecorder = stateRecorder;

            m_UndoStateRecorder.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            m_ToolStateComponent = toolStateComponent;
        }

        /// <summary>
        /// Signals the beginning of an undoable operation. Prepare the object for receiving <see cref="IUndoableStateComponent"/>s.
        /// </summary>
        /// <param name="undoString">The name of the operation, which will be displayed in the undo menu.</param>
        public void BeginOperation(string undoString)
        {
            m_UndoStateRecorder.BeginRecording(undoString);
        }

        /// <summary>
        /// Signals the end of an undoable operation. Sends the undo data to the editor undo system.
        /// </summary>
        public void EndOperation()
        {
            m_UndoStateRecorder.EndRecording(m_ToolStateComponent);
        }

        /// <summary>
        /// Apply the data that was saved on the undo state to the state components, replacing data they were previously holding.
        /// </summary>
        /// <param name="isRedo">True if the context of this call is a redo operation; false if it is an undo operation.</param>
        public void Undo(bool isRedo)
        {
            m_UndoStateRecorder.RestoreState(m_State, isRedo);
        }

        public void StartMergingUndoableCommands()
        {
            m_UndoStateRecorder.StartMergingUndoableCommands();
        }

        public void StopMergingUndoableCommands()
        {
            m_UndoStateRecorder.StopMergingUndoableCommands();
        }

        /// <summary>
        /// Adds the creation of objects to the undo operation.
        /// </summary>
        /// <param name="createdObjects">The created objects.</param>
        public void AddCreatedObjectsUndo(IReadOnlyList<Object> createdObjects)
        {
            m_UndoStateRecorder.AddCreatedObjectsUndo(createdObjects);
        }
    }
}
