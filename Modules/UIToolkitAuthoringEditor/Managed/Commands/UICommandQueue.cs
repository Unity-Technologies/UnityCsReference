// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Global command queue for UI Toolkit authoring commands.
/// Provides a centralized way to enqueue commands and register handlers for specific types or categories.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class UICommandQueue
{
    static readonly CommandSystem s_CommandSystem = new();

    /// <summary>
    /// Registers a handler to receive command execution results for a specific command type.
    /// The handler will only be invoked for the exact type specified. For grouping multiple command types,
    /// use RegisterHandlerForCategory instead.
    /// </summary>
    /// <typeparam name="TCommand">The exact type of command to handle.</typeparam>
    /// <param name="handler">The handler callback to invoke when the command is executed.</param>
    /// <returns>True if the handler was registered; false if it was already registered.</returns>
    public static bool RegisterHandler<TCommand>(CommandSystem.CommandHandler handler)
        where TCommand : Command
        => s_CommandSystem.RegisterHandler<TCommand>(handler);

    /// <summary>
    /// Unregisters a previously registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to stop handling.</typeparam>
    /// <param name="handler">The handler callback to remove.</param>
    /// <returns>True if the handler was removed; false if it was not registered.</returns>
    public static bool UnregisterHandler<TCommand>(CommandSystem.CommandHandler handler)
        where TCommand : Command
        => s_CommandSystem.UnregisterHandler<TCommand>(handler);

    /// <summary>
    /// Registers a handler to receive command execution results for specific category flags.
    /// This provides an alternative to type-based registration that avoids reflection overhead.
    /// Commands must override the Category property to be associated with a category.
    /// The handler will be invoked for any command whose category has ANY overlapping flags with the specified category.
    /// </summary>
    /// <param name="category">The category flags to handle. Can be a combination of flags using bitwise OR.</param>
    /// <param name="handler">The handler callback to invoke when commands with matching categories are executed.</param>
    /// <returns>True if the handler was registered for at least one flag; false if it was already registered for all flags.</returns>
    public static bool RegisterHandlerForCategory(CommandCategory category, CommandSystem.CommandHandler handler)
        => s_CommandSystem.RegisterHandlerForCategory(category, handler);

    /// <summary>
    /// Unregisters a previously registered category handler.
    /// </summary>
    /// <param name="category">The category flags to stop handling.</param>
    /// <param name="handler">The handler callback to remove.</param>
    /// <returns>True if the handler was removed from at least one flag; false if it was not registered for any flag.</returns>
    public static bool UnregisterHandlerForCategory(CommandCategory category, CommandSystem.CommandHandler handler)
        => s_CommandSystem.UnregisterHandlerForCategory(category, handler);

    /// <summary>
    /// Enqueues a command for execution. The command will be automatically disposed after execution completes.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    public static void EnqueueCommand(Command command)
        => s_CommandSystem.EnqueueCommand(command);

    /// <summary>
    /// Begins a new command group. Commands enqueued while the group is active will use the group's
    /// shared undo context for deduplication. Dirty marking is deferred until the outermost group is disposed.
    /// </summary>
    /// <param name="undoName">The undo name for the group. For nested groups, the outermost group's name is used.</param>
    /// <returns>An IDisposable that, when disposed, ends the command group.</returns>
    public static IDisposable BeginGroup(string undoName)
        => s_CommandSystem.BeginGroup(undoName);

    /// <summary>
    /// Gets the number of pooled instances for a specific command type.
    /// For testing purposes only.
    /// </summary>
    internal static int GetPooledCount<TCommand>() where TCommand : Command<TCommand>, new()
        => Command<TCommand>.PooledCount;

    /// <summary>
    /// Clears all pooled instances for a specific command type.
    /// For testing purposes only.
    /// </summary>
    internal static void ClearPool<TCommand>() where TCommand : Command<TCommand>, new()
        => Command<TCommand>.ClearPooledCommands();
}
