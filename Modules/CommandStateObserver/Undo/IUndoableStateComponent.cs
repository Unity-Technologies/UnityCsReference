// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// A state component that can be saved on the undo stack and restored from it.
    /// </summary>
    interface IUndoableStateComponent : IStateComponent
    {
        /// <summary>
        /// A unique id for the state component.
        /// </summary>
        Hash128 Guid { get; }

        /// <summary>
        /// The changeset manager for this state component.
        /// </summary>
        IChangesetManager ChangesetManager { get; }

        /// <summary>
        /// Called before the state component is pushed on the undo stack.
        /// Use this to push additional objects on the stack.
        /// </summary>
        /// <param name="undoString">The name of the undo operation.</param>
        void WillPushOnUndoStack(string undoString);

        /// <summary>
        /// Replaces serialized values of this component by values from <paramref name="undoData"/>.
        /// </summary>
        /// <param name="undoData">The state component from which to take the values.</param>
        /// <param name="changeset"></param>
        void ApplyUndoData(IStateComponent undoData, IChangeset changeset);

        /// <summary>
        /// Called after an undo/redo operation, when the state component can be affected by the operation.
        /// </summary>
        /// <param name="isRedo">True if the operation is a redo, false if the operation is an undo.</param>
        void UndoRedoPerformed(bool isRedo);
    }
}
