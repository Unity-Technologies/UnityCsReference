// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Manages command execution, handler registration, and command grouping.
/// Supports handler registration on specific command types or category-based handler registration
/// for grouping commands by functionality without reflection overhead.
/// Commands are always executed immediately when enqueued. Command groups provide undo scoping:
/// they deduplicate undo object recording and defer dirty marking until the outermost group closes.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal sealed class CommandSystem
{
    public delegate void CommandHandler(in CommandContext context);

    // Pre-filtered to only include single-bit flags (excludes None and composite values)
    static readonly CommandCategory[] s_IndividualFlags = BuildIndividualFlags();

    readonly Dictionary<Type, List<CommandHandler>> m_Handlers = new();
    readonly Dictionary<CommandCategory, List<CommandHandler>> m_CategoryHandlers = new();

    HashSet<UnityEngine.Object> m_ActiveGroupUndoObjects;
    string m_OutermostGroupUndoName;
    int m_CommandGroupDepth;

    const int k_MaxScopePoolSize = 4;
    readonly Stack<CommandGroupScope> m_ScopePool = new();

    /// <summary>
    /// Registers a handler to receive command execution results for a specific command type.
    /// The handler will only be invoked for the exact type specified. For grouping multiple command types,
    /// use RegisterHandlerForCategory instead.
    /// </summary>
    /// <typeparam name="TCommand">The exact type of command to handle.</typeparam>
    /// <param name="handler">The handler callback to invoke when the command is executed.</param>
    /// <returns>True if the handler was registered; false if it was already registered.</returns>
    public bool RegisterHandler<TCommand>(CommandHandler handler)
        where TCommand : Command
    {
        Assert.IsNotNull(handler, "Handler cannot be null");

        var commandType = typeof(TCommand);
        if (!m_Handlers.TryGetValue(commandType, out var handlerList))
        {
            handlerList = new List<CommandHandler>();
            m_Handlers[commandType] = handlerList;
        }

        if (handlerList.Contains(handler))
            return false;

        handlerList.Add(handler);
        return true;
    }

    /// <summary>
    /// Unregisters a previously registered handler.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to stop handling.</typeparam>
    /// <param name="handler">The handler callback to remove.</param>
    /// <returns>True if the handler was removed; false if it was not registered.</returns>
    public bool UnregisterHandler<TCommand>(CommandHandler handler)
        where TCommand : Command
    {
        Assert.IsNotNull(handler, "Handler cannot be null");

        var commandType = typeof(TCommand);
        if (!m_Handlers.TryGetValue(commandType, out var handlerList))
            return false;

        if (!handlerList.Remove(handler))
            return false;

        if (handlerList.Count == 0)
            m_Handlers.Remove(commandType);

        return true;
    }

    /// <summary>
    /// Registers a handler to receive command execution results for specific category flags.
    /// This provides an alternative to type-based registration that avoids reflection overhead.
    /// Commands must override the Category property to be associated with a category.
    /// The handler will be invoked for any command whose category has ANY overlapping flags with the specified category.
    /// If multiple flags are specified, the handler is registered once for each individual flag.
    /// </summary>
    /// <param name="category">The category flags to handle. Can be a combination of flags using bitwise OR.</param>
    /// <param name="handler">The handler callback to invoke when commands with matching categories are executed.</param>
    /// <returns>True if the handler was registered for at least one flag; false if it was already registered for all flags.</returns>
    public bool RegisterHandlerForCategory(CommandCategory category, CommandHandler handler)
    {
        Assert.IsTrue(category != CommandCategory.None, "Category cannot be None");
        Assert.IsNotNull(handler, "Handler cannot be null");

        var registered = false;
        for (var i = 0; i < s_IndividualFlags.Length; i++)
        {
            var flag = s_IndividualFlags[i];
            if ((category & flag) == 0)
                continue;

            if (!m_CategoryHandlers.TryGetValue(flag, out var handlerList))
            {
                handlerList = new List<CommandHandler>();
                m_CategoryHandlers[flag] = handlerList;
            }

            if (handlerList.Contains(handler))
                continue;

            handlerList.Add(handler);
            registered = true;
        }

        return registered;
    }

    /// <summary>
    /// Unregisters a previously registered category handler.
    /// If multiple flags were specified during registration, the handler is removed from each individual flag.
    /// </summary>
    /// <param name="category">The category flags to stop handling.</param>
    /// <param name="handler">The handler callback to remove.</param>
    /// <returns>True if the handler was removed from at least one flag; false if it was not registered for any flag.</returns>
    public bool UnregisterHandlerForCategory(CommandCategory category, CommandHandler handler)
    {
        Assert.IsTrue(category != CommandCategory.None, "Category cannot be None");
        Assert.IsNotNull(handler, "Handler cannot be null");

        var removed = false;
        for (var i = 0; i < s_IndividualFlags.Length; i++)
        {
            var flag = s_IndividualFlags[i];
            if ((category & flag) == 0)
                continue;

            if (!m_CategoryHandlers.TryGetValue(flag, out var handlerList))
                continue;

            if (!handlerList.Remove(handler))
                continue;

            if (handlerList.Count == 0)
                m_CategoryHandlers.Remove(flag);

            removed = true;
        }

        return removed;
    }

    /// <summary>
    /// Enqueues a command for execution. Commands are always executed immediately.
    /// When inside a command group, the group's undo context is used for deduplication and dirty deferral.
    /// The command system acquires a reference to the command for the duration of execution and releases
    /// it afterward. The caller retains ownership and is responsible for disposing the command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    public void EnqueueCommand(Command command)
    {
        Assert.IsNotNull(command, "Command cannot be null");
        command.Acquire();
        ExecuteCommandAndInvokeHandlers(command);
    }

    /// <summary>
    /// Begins a new command group. Commands enqueued while the group is active will use the group's
    /// shared undo context for deduplication. Dirty marking is deferred until the outermost group is disposed.
    /// </summary>
    /// <param name="undoName">The undo name for the group. For nested groups, the outermost group's name is used.</param>
    /// <returns>An IDisposable that, when disposed, ends the command group.</returns>
    public IDisposable BeginGroup(string undoName)
    {
        BeginCommandGroup(undoName);
        var scope = m_ScopePool.Count > 0 ? m_ScopePool.Pop() : new CommandGroupScope(this);
        scope.m_Disposed = false;
        return scope;
    }

    /// <summary>
    /// Begins a new command group. Commands enqueued while the group is active will use the group's
    /// shared undo context for deduplication. Dirty marking is deferred until the outermost group closes.
    /// </summary>
    /// <param name="undoName">The undo name for the group. For nested groups, the outermost group's name is used.</param>
    internal void BeginCommandGroup(string undoName)
    {
        Assert.IsFalse(string.IsNullOrEmpty(undoName), "Undo name is required for command groups");

        if (m_CommandGroupDepth == 0)
        {
            if (m_ActiveGroupUndoObjects == null)
                m_ActiveGroupUndoObjects = new HashSet<UnityEngine.Object>();
            else
                m_ActiveGroupUndoObjects.Clear();

            m_OutermostGroupUndoName = undoName;
        }

        m_CommandGroupDepth++;
    }

    /// <summary>
    /// Ends the current command group. When the outermost group closes, all tracked objects are marked dirty.
    /// </summary>
    internal void EndCommandGroup()
    {
        Assert.IsTrue(m_CommandGroupDepth > 0, "EndCommandGroup called without a matching BeginCommandGroup");

        m_CommandGroupDepth--;

        if (m_CommandGroupDepth == 0 && m_ActiveGroupUndoObjects is { Count: > 0 })
        {
            foreach (var obj in m_ActiveGroupUndoObjects)
            {
                if (obj != null)
                    EditorUtility.SetDirty(obj);
            }

            m_ActiveGroupUndoObjects.Clear();
        }
    }

    void ExecuteCommandAndInvokeHandlers(Command command)
    {
        if (!command.Validate())
        {
            try
            {
                var failedContext = new CommandContext(command, command.Source, CommandExecutionStatus.ValidationFailed);
                InvokeHandlers(command, failedContext);
            }
            finally
            {
                command.Release();
            }
            return;
        }

        var insideGroup = m_CommandGroupDepth > 0;
        var undoObjects = insideGroup ? m_ActiveGroupUndoObjects : HashSetPool<UnityEngine.Object>.Get();

        try
        {
            var undoName = insideGroup ? m_OutermostGroupUndoName : command.UndoName;
            var prepareContext = new PrepareContext(undoObjects, undoName);
            command.Prepare(in prepareContext);

            CommandExecutionStatus status;
            try
            {
                status = command.Execute();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                status = CommandExecutionStatus.ExecutionFailed;
            }

            if (!insideGroup)
            {
                foreach (var obj in undoObjects)
                {
                    if (obj != null)
                        EditorUtility.SetDirty(obj);
                }
            }

            var context = new CommandContext(command, command.Source, status);
            InvokeHandlers(command, context);
        }
        finally
        {
            command.Release();
            if (!insideGroup)
                HashSetPool<UnityEngine.Object>.Release(undoObjects);
        }
    }

    void InvokeHandlers(Command command, CommandContext context)
    {
        using var snapshotHandle = ListPool<CommandHandler>.Get(out var handlerSnapshot);

        var commandCategory = command.Category;
        HashSet<CommandHandler> invokedHandlers = null;

        try
        {
            if (commandCategory != CommandCategory.None)
            {
                for (var i = 0; i < s_IndividualFlags.Length; i++)
                {
                    var flag = s_IndividualFlags[i];
                    if ((commandCategory & flag) == 0)
                        continue;

                    if (!m_CategoryHandlers.TryGetValue(flag, out var handlerList))
                        continue;

                    invokedHandlers ??= HashSetPool<CommandHandler>.Get();
                    handlerSnapshot.AddRange(handlerList);
                    for (var j = 0; j < handlerSnapshot.Count; j++)
                    {
                        var categoryHandler = handlerSnapshot[j];
                        if (invokedHandlers.Add(categoryHandler))
                            categoryHandler.Invoke(in context);
                    }

                    handlerSnapshot.Clear();
                }
            }

            var commandType = command.GetType();
            if (!m_Handlers.TryGetValue(commandType, out var typeHandlerList))
                return;

            handlerSnapshot.AddRange(typeHandlerList);
            for (var i = 0; i < handlerSnapshot.Count; i++)
            {
                var typeHandler = handlerSnapshot[i];
                if (invokedHandlers == null || invokedHandlers.Add(typeHandler))
                    typeHandler.Invoke(in context);
            }
        }
        finally
        {
            if (invokedHandlers != null)
                HashSetPool<CommandHandler>.Release(invokedHandlers);
        }
    }

    static CommandCategory[] BuildIndividualFlags()
    {
        var allValues = (CommandCategory[])Enum.GetValues(typeof(CommandCategory));
        var flags = new List<CommandCategory>();
        for (var i = 0; i < allValues.Length; i++)
        {
            var value = (int)allValues[i];
            if (value > 0 && (value & (value - 1)) == 0)
                flags.Add(allValues[i]);
        }
        return flags.ToArray();
    }

    sealed class CommandGroupScope(CommandSystem commandSystem) : IDisposable
    {
        internal bool m_Disposed;
        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            commandSystem.EndCommandGroup();
            if (commandSystem.m_ScopePool.Count < k_MaxScopePoolSize)
                commandSystem.m_ScopePool.Push(this);
        }
    }
}
