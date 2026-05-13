// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents the execution status of a command.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal enum CommandExecutionStatus
{
    /// <summary>
    /// The command was successfully validated and executed.
    /// </summary>
    Success,

    /// <summary>
    /// The command failed validation and was not executed.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// The command failed execution and was not completed.
    /// </summary>
    ExecutionFailed
}


/// <summary>
/// Context for preparing a command before execution. Handles deduplication and immediate undo registration.
/// </summary>
readonly ref struct PrepareContext
{
    readonly HashSet<UnityEngine.Object> m_UndoObjects;
    readonly string m_UndoName;

    internal PrepareContext(HashSet<UnityEngine.Object> undoObjects, string undoName)
    {
        m_UndoObjects = undoObjects;
        m_UndoName = undoName;
    }

    /// <summary>
    /// Records an object that will be modified by the command, for undo system registration.
    /// The object is deduplicated and undo is registered immediately.
    /// </summary>
    /// <param name="obj">The object to record for undo. Null values are ignored.</param>
    public void RecordUndo(UnityEngine.Object obj)
    {
        if (obj != null && m_UndoObjects.Add(obj))
            Undo.RegisterCompleteObjectUndo(obj, m_UndoName);
    }

    /// <summary>
    /// Records multiple objects that will be modified by the command, for undo system registration.
    /// </summary>
    /// <param name="objects">The span of objects to record for undo. Null values are ignored.</param>
    public void RecordUndo(ReadOnlySpan<UnityEngine.Object> objects)
    {
        foreach (var obj in objects)
        {
            if (obj != null && m_UndoObjects.Add(obj))
                Undo.RegisterCompleteObjectUndo(obj, m_UndoName);
        }
    }
}

/// <summary>
/// Contains the context information for a command execution, including the command, source object, and execution status.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal readonly ref struct CommandContext
{
    /// <summary>
    /// The command that was executed.
    /// </summary>
    public readonly Command Command;

    /// <summary>
    /// The source object that originated the command, if any.
    /// </summary>
    public readonly object Source;

    /// <summary>
    /// The execution status of the command.
    /// </summary>
    public readonly CommandExecutionStatus Status;

    /// <summary>
    /// Initializes a new instance of the CommandContext struct.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="source">The source object that originated the command.</param>
    /// <param name="status">The execution status of the command.</param>
    public CommandContext(Command command, object source, CommandExecutionStatus status)
    {
        Command = command;
        Source = source;
        Status = status;
    }
}
