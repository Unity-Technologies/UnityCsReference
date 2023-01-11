// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A state component to store undo information.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class UndoStateComponent : StateComponent<UndoStateComponent.StateUpdater>, IDisposable
    {
        /// <summary>
        /// Updater for <see cref="UndoStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<UndoStateComponent>
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
        ToolStateComponent m_ToolStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoStateComponent"/> class.
        /// </summary>
        public UndoStateComponent(IState state, ToolStateComponent toolStateComponent)
        {
            m_State = state;
            m_UndoStateRecorder = ScriptableObject.CreateInstance<UndoStateRecorder>();
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

        /// <inheritdoc />
        public override void OnRemovedFromState(IState state)
        {
            base.OnRemovedFromState(state);
            Dispose();
        }

        /// <summary>
        /// Apply the data that was saved on the undo state to the state components, replacing data they were previously holding.
        /// </summary>
        /// <param name="isRedo">True if the context of this call is a redo operation; false if it is an undo operation.</param>
        public void Undo(bool isRedo)
        {
            m_UndoStateRecorder.RestoreState(m_State, isRedo);
        }

        /// <summary>
        /// Implements the Dispose operation.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_UndoStateRecorder != null)
            {
                Object.DestroyImmediate(m_UndoStateRecorder);
                m_UndoStateRecorder = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~UndoStateComponent()
        {
            Dispose(false);
        }
    }
}
