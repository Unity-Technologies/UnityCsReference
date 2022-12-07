// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Interface for merging multiple commands into a single undo operation.
/// </summary>
interface IUndoableCommandMerger
{
    /// <summary>
    /// Indicate that you want to merge the next undoable commands into one undo.
    /// </summary>
    void StartMerging();

    /// <summary>
    /// Ends the merging of undoables commands into one undo.
    /// </summary>
    void StopMerging();
}
