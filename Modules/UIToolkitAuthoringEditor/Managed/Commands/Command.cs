// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Abstract base class for all commands. Commands are pooled objects with a lifecycle similar to UI Toolkit events.
/// Concrete command types should inherit from Command&lt;T&gt; instead of directly from Command.
/// Use TCommand.GetPooled() to acquire a command from the pool, set its properties, then dispatch it via CommandSystem.
/// The command will be automatically returned to the pool after execution.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
abstract class Command : IDisposable
{
    int m_ReferenceCount;

    /// <summary>
    /// Gets or sets the source object that originated the command.
    /// Typically set to a reference type identifying the origin (e.g., a UI element, window, or source marker).
    /// </summary>
    /// <remarks>
    /// Note: Value types will be boxed. Use reference types for source tracking to avoid allocations.
    /// </remarks>
    public object Source { get; set; }

    /// <summary>
    /// Gets the optional category flags for this command. When set, handlers registered for matching
    /// categories will be invoked in addition to type-based handlers. Override this property to assign
    /// one or more categories using bitwise OR (e.g., CommandCategory.InlineRule | CommandCategory.StyleSheetRule).
    /// </summary>
    public virtual CommandCategory Category => CommandCategory.None;

    /// <summary>
    /// Gets the name displayed in the undo history for this command.
    /// Override to provide a meaningful description of what the command does.
    /// </summary>
    public virtual string UndoName => "Command";

    /// <summary>
    /// Initializes the command. Called when the command is acquired from the pool.
    /// Override to reset command state to defaults.
    /// </summary>
    protected virtual void Init()
    {
        Source = null;
    }

    /// <summary>
    /// Prepares the command for execution by registering objects that will be modified.
    /// Override this method to call context.RecordUndo() for any objects that will be changed.
    /// This is called before Execute() to allow proper undo recording.
    /// </summary>
    /// <param name="context">The preparation context for registering undo objects.</param>
    public virtual void Prepare(in PrepareContext context)
    {
    }

    /// <summary>
    /// Validates whether the command can be executed.
    /// </summary>
    /// <returns>True if the command can be executed; otherwise, false.</returns>
    public virtual bool Validate() => true;

    /// <summary>
    /// Executes the command and returns the execution status.
    /// </summary>
    /// <returns>The CommandExecutionStatus indicating the execution result.</returns>
    public virtual CommandExecutionStatus Execute() => CommandExecutionStatus.Success;

    /// <summary>
    /// Acquires a reference to this command, incrementing the reference count.
    /// </summary>
    internal void Acquire()
    {
        m_ReferenceCount++;
        if (m_ReferenceCount == 1)
            Init();
    }

    /// <summary>
    /// Releases a reference to this command. When the reference count reaches zero,
    /// the command is returned to the pool.
    /// </summary>
    internal void Release()
    {
        m_ReferenceCount--;
        if (m_ReferenceCount == 0)
            ReleaseToPool();
    }

    /// <summary>
    /// Returns the command to its type-specific pool. Implemented by Command&lt;T&gt;.
    /// </summary>
    protected abstract void ReleaseToPool();

    /// <summary>
    /// Disposes the command, releasing it back to the pool.
    /// </summary>
    public void Dispose()
    {
        Release();
    }
}

/// <summary>
/// Generic base class for commands, implementing command pooling.
/// Concrete command types should inherit from this class using the Curiously Recurring Template Pattern (CRTP).
/// </summary>
/// <typeparam name="T">The concrete command type (must be the inheriting class itself).</typeparam>
/// <example>
/// <code>
/// // Correct usage:
/// class MyCommand : Command&lt;MyCommand&gt;
/// {
///     public override bool Validate() => true;
///     public override CommandExecutionStatus Execute() { ... }
/// }
///
/// // Incorrect usage (will not compile):
/// class MyCommand : Command&lt;SomeOtherCommand&gt; // ERROR: Type constraint violated
/// </code>
/// </example>
/// <remarks>
/// The CRTP pattern ensures each command type gets its own static pool while maintaining type safety.
/// The constraint 'where T : Command&lt;T&gt;, new()' enforces that T must be the same type as the inheriting class.
/// </remarks>
internal abstract class Command<T> : Command where T : Command<T>, new()
{
    static readonly ObjectPool<T> s_Pool = new(() => new T());
    internal static int PooledCount => s_Pool.Size();
    internal static void ClearPooledCommands() => s_Pool.Clear();

    /// <summary>
    /// Allows providing a custom function to create the command instance.
    /// This avoids the overhead of the default constructor or Activator.CreateInstance.
    /// </summary>
    /// <param name="createMethod">The creation function.</param>
    public static void SetCreateFunction(Func<T> createMethod)
    {
        s_Pool.CreateFunc = createMethod;
    }

    /// <summary>
    /// Gets a pooled command instance.
    /// </summary>
    /// <returns>A command instance from the pool.</returns>
    public static T GetPooled()
    {
        var command = s_Pool.Get();
        command.Acquire();
        return command;
    }

    /// <summary>
    /// Returns this command to its type-specific pool.
    /// </summary>
    protected override void ReleaseToPool()
    {
        Init();
        s_Pool.Release((T)this);
    }
}
