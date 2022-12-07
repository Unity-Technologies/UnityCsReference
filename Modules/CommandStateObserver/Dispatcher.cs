// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Diagnostic flags for command dispatch.
    /// </summary>
    [Flags]
    enum Diagnostics
    {
        /// <summary>
        /// No diagnostic done when dispatching.
        /// </summary>
        None = 0,

        /// <summary>
        /// Log all dispatched commands and their handler.
        /// </summary>
        LogAllCommands = 1 << 0,

        /// <summary>
        /// Log an error when a command is dispatched while dispatching another command.
        /// </summary>
        CheckRecursiveDispatch = 1 << 1,
    }

    /// <summary>
    /// The command dispatcher.
    /// </summary>
    class Dispatcher : ICommandTarget
    {
        /// <summary>
        /// The mapping of command types to command handlers.
        /// </summary>
        protected readonly Dictionary<Type, ICommandHandlerFunctor> m_CommandHandlers = new Dictionary<Type, ICommandHandlerFunctor>();

        /// <summary>
        /// The list of actions that needs to be invoked before executing a command.
        /// </summary>
        protected readonly List<Action<ICommand>> m_CommandPreDispatchCallbacks = new List<Action<ICommand>>();

        /// <summary>
        /// The command being executed.
        /// </summary>
        protected ICommand m_CurrentCommand;

        /// <summary>
        /// Returns true is a command is being dispatched.
        /// </summary>
        protected bool IsDispatching => m_CurrentCommand != null;

        /// <summary>
        /// Registers a handler for a command type. Replaces any previously registered handler for the command type.
        /// </summary>
        /// <param name="commandHandlerFunctor">The command handler.</param>
        public void RegisterCommandHandler<TCommand>(ICommandHandlerFunctor commandHandlerFunctor) where TCommand : ICommand
        {
            if (!IsDispatching)
            {
                var commandType = typeof(TCommand);
                m_CommandHandlers[commandType] = commandHandlerFunctor;
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(RegisterCommandHandler)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Gets the command handler for a command type.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        /// <returns>The command handler for the command, or null if there is none.</returns>
        public ICommandHandlerFunctor GetCommandHandler<TCommand>() where TCommand : ICommand
        {
            return GetCommandHandler(typeof(TCommand));
        }

        /// <summary>
        /// Gets the command handler for a command type.
        /// </summary>
        /// <param name="commandType">The type of the command.</param>
        /// <returns>The command handler for the command, or null if there is none.</returns>
        public ICommandHandlerFunctor GetCommandHandler(Type commandType)
        {
            return m_CommandHandlers.TryGetValue(commandType, out var functor) ? functor : null;
        }

        /// <summary>
        /// Unregisters the command handler for a command type.
        /// </summary>
        /// <remarks>
        /// Since there is only one command handler registered for a command type, it is not necessary
        /// to specify the command handler to unregister.
        /// </remarks>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void UnregisterCommandHandler<TCommand>() where TCommand : ICommand
        {
            if (!IsDispatching)
            {
                m_CommandHandlers.Remove(typeof(TCommand));
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(UnregisterCommandHandler)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Registers a method to be called before dispatching a command.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <exception cref="InvalidOperationException">Thrown when the callback is already registered.</exception>
        public void RegisterCommandPreDispatchCallback(Action<ICommand> callback)
        {
            if (!IsDispatching)
            {
                if (m_CommandPreDispatchCallbacks.Contains(callback))
                    throw new InvalidOperationException("Cannot register the same pre-dispatch callback twice.");
                m_CommandPreDispatchCallbacks.Add(callback);
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(RegisterCommandPreDispatchCallback)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Unregisters a pre-dispatch callback.
        /// </summary>
        /// <param name="callback">The callback.</param>
        public void UnregisterCommandPreDispatchCallback(Action<ICommand> callback)
        {
            if (!IsDispatching)
            {
                m_CommandPreDispatchCallbacks.Remove(callback);
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(UnregisterCommandPreDispatchCallback)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Dispatches a command: the command handler registered for this command will be executed.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Flags to control logging.</param>
        public virtual void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (IsDispatching && (diagnosticsFlags & Diagnostics.CheckRecursiveDispatch) == Diagnostics.CheckRecursiveDispatch)
            {
                Debug.LogError($"Recursive dispatch detected: command {command.GetType().Name} dispatched during {m_CurrentCommand.GetType().Name}'s dispatch");
            }

            try
            {
                m_CurrentCommand = command;

                foreach (var callback in m_CommandPreDispatchCallbacks)
                {
                    callback(command);
                }

                PreDispatchCommand(command);

                try
                {
                    var logHandler = (diagnosticsFlags & Diagnostics.LogAllCommands) == Diagnostics.LogAllCommands;

                    var handler = GetCommandHandler(command.GetType());
                    handler?.Invoke(command, logHandler);
                }
                finally
                {
                    PostDispatchCommand(command);
                }
            }
            finally
            {
                m_CurrentCommand = null;
            }
        }

        /// <summary>
        /// Called when a command is dispatched, before the command handler is executed,
        /// but after the pre-dispatch callbacks.
        /// </summary>
        /// <param name="command">The command being dispatched.</param>
        protected virtual void PreDispatchCommand(ICommand command)
        {
        }

        /// <summary>
        /// Called when a command is dispatched, after the command handler has been executed.
        /// </summary>
        /// <param name="command">The command being dispatched.</param>
        protected virtual void PostDispatchCommand(ICommand command)
        {
        }
    }
}
