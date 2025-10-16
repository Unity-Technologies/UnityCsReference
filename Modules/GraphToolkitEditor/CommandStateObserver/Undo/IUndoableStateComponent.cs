// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// A state component that can be saved on the undo stack and restored from it.
    /// </summary>
    [UnityRestricted]
    internal interface IUndoableStateComponent : IStateComponent
    {
        /// <summary>
        /// A unique id for the state component.
        /// </summary>
        Hash128 Guid { get; }

        /// <summary>
        /// Called before the state component is pushed on the undo stack.
        /// Use this to push additional objects on the stack.
        /// </summary>
        /// <param name="undoString">The name of the undo operation.</param>
        void WillPushOnUndoStack(string undoString);

        /// <summary>
        /// Whether <paramref name="newStateComponent"/> can be used as a data source for <see cref="ApplyUndoData"/>.
        /// </summary>
        /// <param name="newStateComponent">The state component to check.</param>
        bool CanBeUndoDataSource(IUndoableStateComponent newStateComponent) => newStateComponent.Guid == Guid;

        /// <summary>
        /// Replaces serialized values of this component by values from <paramref name="undoData"/>.
        /// </summary>
        /// <param name="undoData">The state component from which to take the values.</param>
        /// <param name="changeset">A description of the changes brought in by <paramref name="undoData"/>.
        /// If null, anything may have changed.</param>
        void ApplyUndoData(IStateComponent undoData, IChangeset changeset);

        /// <summary>
        /// Called after an undo/redo operation, when the state component can be affected by the operation.
        /// </summary>
        /// <param name="isRedo">True if the operation is a redo, false if the operation is an undo.</param>
        void UndoRedoPerformed(bool isRedo);
    }
}
