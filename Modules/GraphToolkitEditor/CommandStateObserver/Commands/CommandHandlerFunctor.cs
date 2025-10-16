// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TCommand">The command type.</typeparam>
    [UnityRestricted]
    internal delegate void CommandHandler<in TCommand>(TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="param">The parameter to pass to the handler.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TParam">The type of the handler parameter.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    [UnityRestricted]
    internal delegate void CommandHandler<in TParam, in TCommand>(TParam param, TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="param1">The first parameter to pass to the handler.</param>
    /// <param name="param2">The second parameter to pass to the handler.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TParam1">The type of the handler first parameter.</typeparam>
    /// <typeparam name="TParam2">The type of the handler second parameter.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    [UnityRestricted]
    internal delegate void CommandHandler<in TParam1, in TParam2, in TCommand>(TParam1 param1, TParam2 param2, TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="param1">The first parameter to pass to the handler.</param>
    /// <param name="param2">The second parameter to pass to the handler.</param>
    /// <param name="param3">The third parameter to pass to the handler.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TParam1">The type of the handler first parameter.</typeparam>
    /// <typeparam name="TParam2">The type of the handler second parameter.</typeparam>
    /// <typeparam name="TParam3">The type of the handler third parameter.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    [UnityRestricted]
    internal delegate void CommandHandler<in TParam1, in TParam2, in TParam3, in TCommand>(TParam1 param1, TParam2 param2, TParam3 param3, TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="param1">The first parameter to pass to the handler.</param>
    /// <param name="param2">The second parameter to pass to the handler.</param>
    /// <param name="param3">The third parameter to pass to the handler.</param>
    /// <param name="param4">The fourth parameter to pass to the handler.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TParam1">The type of the handler first parameter.</typeparam>
    /// <typeparam name="TParam2">The type of the handler second parameter.</typeparam>
    /// <typeparam name="TParam3">The type of the handler third parameter.</typeparam>
    /// <typeparam name="TParam4">The type of the handler fourth parameter.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    [UnityRestricted]
    internal delegate void CommandHandler<in TParam1, in TParam2, in TParam3, in TParam4, in TCommand>(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="param1">The first parameter to pass to the handler.</param>
    /// <param name="param2">The second parameter to pass to the handler.</param>
    /// <param name="param3">The third parameter to pass to the handler.</param>
    /// <param name="param4">The fourth parameter to pass to the handler.</param>
    /// <param name="param5">The fifth parameter to pass to the handler.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TParam1">The type of the handler first parameter.</typeparam>
    /// <typeparam name="TParam2">The type of the handler second parameter.</typeparam>
    /// <typeparam name="TParam3">The type of the handler third parameter.</typeparam>
    /// <typeparam name="TParam4">The type of the handler fourth parameter.</typeparam>
    /// <typeparam name="TParam5">The type of the handler fifth parameter.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    [UnityRestricted]
    internal delegate void CommandHandler<in TParam1, in TParam2, in TParam3, in TParam4, in TParam5, in TCommand>(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TCommand}"/> and invoke it.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerFunctor<TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandler<TCommand> m_Callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TCommand> callback = null)
        {
            m_Callback = callback ?? CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TCommand>, TCommand>();
        }

        /// <inheritdoc />
        public Type CommandType => typeof(TCommand);

        /// <inheritdoc />
        public ICommandHandlerFunctor Bind(List<IStateComponent> args)
        {
            if (args.Count != 0)
            {
                throw new ArgumentException("Expected 0 argument for binding.");
            }

            return this;
        }

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        public void Invoke(ICommand command, bool logHandler)
        {
            if (m_Callback == null)
            {
                return;
            }

            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            var tCommand = (TCommand)command;
            m_Callback(tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam, TCommand}"/>, bind its parameter and invoke it.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerFunctor<TParam, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandlerUtilities.BindingState m_BindingState;
        CommandHandler<TParam, TCommand> m_Callback;
        TParam m_HandlerParam;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam, TCommand> callback = null)
        {
            m_Callback = callback ?? CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam, TCommand>, TCommand>();
            m_BindingState = CommandHandlerUtilities.BindingState.Unbound;
        }

        /// <inheritdoc />
        public Type CommandType => typeof(TCommand);

        /// <inheritdoc />
        public ICommandHandlerFunctor Bind(List<IStateComponent> args)
        {
            if (args.Count != 1)
            {
                throw new ArgumentException("Expected 1 argument for binding.");
            }

            return Bind((TParam)args[0]);
        }

        /// <summary>
        /// Binds the handler parameter.
        /// </summary>
        /// <param name="handlerParam">The object to bind to the handler parameter.</param>
        /// <returns>The current command handler functor.</returns>
        public CommandHandlerFunctor<TParam, TCommand> Bind(TParam handlerParam)
        {
            m_HandlerParam = handlerParam;
            m_BindingState = CommandHandlerUtilities.BindingState.Bound;
            return this;
        }

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        public void Invoke(ICommand command, bool logHandler)
        {
            if (m_Callback == null)
            {
                return;
            }

            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            if (m_BindingState != CommandHandlerUtilities.BindingState.Bound)
            {
                throw new InvalidOperationException("Cannot invoke an unbound CommandHandlerFunctor.");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerFunctor<TParam1, TParam2, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandlerUtilities.BindingState m_BindingState;
        CommandHandler<TParam1, TParam2, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TCommand> callback = null)
        {
            m_Callback = callback ?? CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TCommand>, TCommand>();
            m_BindingState = CommandHandlerUtilities.BindingState.Unbound;
        }

        /// <inheritdoc />
        public Type CommandType => typeof(TCommand);

        /// <inheritdoc />
        public ICommandHandlerFunctor Bind(List<IStateComponent> args)
        {
            if (args.Count != 2)
            {
                throw new ArgumentException("Expected 2 arguments for binding.");
            }

            return Bind((TParam1)args[0], (TParam2)args[1]);
        }

        /// <summary>
        /// Binds the handler parameters.
        /// </summary>
        /// <param name="handlerParam1">The object to bind to the handler first parameter.</param>
        /// <param name="handlerParam2">The object to bind to the handler second parameter.</param>
        /// <returns>The current command handler functor.</returns>
        public CommandHandlerFunctor<TParam1, TParam2, TCommand>
            Bind(TParam1 handlerParam1, TParam2 handlerParam2)
        {
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
            m_BindingState = CommandHandlerUtilities.BindingState.Bound;
            return this;
        }

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        public void Invoke(ICommand command, bool logHandler)
        {
            if (m_Callback == null)
            {
                return;
            }

            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            if (m_BindingState != CommandHandlerUtilities.BindingState.Bound)
            {
                throw new InvalidOperationException("Cannot invoke an unbound CommandHandlerFunctor.");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandlerUtilities.BindingState m_BindingState;
        CommandHandler<TParam1, TParam2, TParam3, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;
        TParam3 m_HandlerParam3;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TParam3, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TCommand> callback = null)
        {
            m_Callback = callback ?? CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TParam3, TCommand>, TCommand>();
            m_BindingState = CommandHandlerUtilities.BindingState.Unbound;
        }

        /// <inheritdoc />
        public Type CommandType => typeof(TCommand);

        /// <inheritdoc />
        public ICommandHandlerFunctor Bind(List<IStateComponent> args)
        {
            if (args.Count != 3)
            {
                throw new ArgumentException("Expected 3 arguments for binding.");
            }

            return Bind((TParam1)args[0], (TParam2)args[1], (TParam3)args[2]);
        }

        /// <summary>
        /// Binds the handler parameters.
        /// </summary>
        /// <param name="handlerParam1">The object to bind to the handler first parameter.</param>
        /// <param name="handlerParam2">The object to bind to the handler second parameter.</param>
        /// <param name="handlerParam3">The object to bind to the handler third parameter.</param>
        /// <returns>The current command handler functor.</returns>
        public CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand>
            Bind(TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3)
        {
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
            m_HandlerParam3 = handlerParam3;
            m_BindingState = CommandHandlerUtilities.BindingState.Bound;
            return this;
        }

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        public void Invoke(ICommand command, bool logHandler)
        {
            if (m_Callback == null)
            {
                return;
            }

            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            if (m_BindingState != CommandHandlerUtilities.BindingState.Bound)
            {
                throw new InvalidOperationException("Cannot invoke an unbound CommandHandlerFunctor.");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, m_HandlerParam3, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TParam4, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandlerUtilities.BindingState m_BindingState;
        CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;
        TParam3 m_HandlerParam3;
        TParam4 m_HandlerParam4;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TParam3, TParam4, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> callback = null)
        {
            m_Callback = callback ?? CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand>, TCommand>();
            m_BindingState = CommandHandlerUtilities.BindingState.Unbound;
        }

        /// <inheritdoc />
        public Type CommandType => typeof(TCommand);

        /// <inheritdoc />
        public ICommandHandlerFunctor Bind(List<IStateComponent> args)
        {
            if (args.Count != 4)
            {
                throw new ArgumentException("Expected 4 arguments for binding.");
            }

            return Bind((TParam1)args[0], (TParam2)args[1], (TParam3)args[2], (TParam4)args[3]);
        }

        /// <summary>
        /// Binds the handler parameters.
        /// </summary>
        /// <param name="handlerParam1">The object to bind to the handler first parameter.</param>
        /// <param name="handlerParam2">The object to bind to the handler second parameter.</param>
        /// <param name="handlerParam3">The object to bind to the handler third parameter.</param>
        /// <param name="handlerParam4">The object to bind to the handler fourth parameter.</param>
        /// <returns>The current command handler functor.</returns>
        public CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand>
            Bind(TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3, TParam4 handlerParam4)
        {
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
            m_HandlerParam3 = handlerParam3;
            m_HandlerParam4 = handlerParam4;
            m_BindingState = CommandHandlerUtilities.BindingState.Bound;
            return this;
        }

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        public void Invoke(ICommand command, bool logHandler)
        {
            if (m_Callback == null)
            {
                return;
            }

            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            if (m_BindingState != CommandHandlerUtilities.BindingState.Bound)
            {
                throw new InvalidOperationException("Cannot invoke an unbound CommandHandlerFunctor.");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, m_HandlerParam3, m_HandlerParam4, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TParam4, TParam5, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandlerUtilities.BindingState m_BindingState;
        CommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;
        TParam3 m_HandlerParam3;
        TParam4 m_HandlerParam4;
        TParam5 m_HandlerParam5;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TParam3, TParam4, TParam5, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand> callback = null)
        {
            m_Callback = callback ?? CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand>, TCommand>();
            m_BindingState = CommandHandlerUtilities.BindingState.Unbound;
        }

        /// <inheritdoc />
        public Type CommandType => typeof(TCommand);

        /// <inheritdoc />
        public ICommandHandlerFunctor Bind(List<IStateComponent> args)
        {
            if (args.Count != 5)
            {
                throw new ArgumentException("Expected 5 arguments for binding.");
            }

            return Bind((TParam1)args[0], (TParam2)args[1], (TParam3)args[2], (TParam4)args[3], (TParam5)args[4]);
        }

        /// <summary>
        /// Binds the handler parameters.
        /// </summary>
        /// <param name="handlerParam1">The object to bind to the handler first parameter.</param>
        /// <param name="handlerParam2">The object to bind to the handler second parameter.</param>
        /// <param name="handlerParam3">The object to bind to the handler third parameter.</param>
        /// <param name="handlerParam4">The object to bind to the handler fourth parameter.</param>
        /// <param name="handlerParam5">The object to bind to the handler fifth parameter.</param>
        /// <returns>The current command handler functor.</returns>
        public CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand>
            Bind(TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3, TParam4 handlerParam4, TParam5 handlerParam5)
        {
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
            m_HandlerParam3 = handlerParam3;
            m_HandlerParam4 = handlerParam4;
            m_HandlerParam5 = handlerParam5;
            m_BindingState = CommandHandlerUtilities.BindingState.Bound;
            return this;
        }

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        public void Invoke(ICommand command, bool logHandler)
        {
            if (m_Callback == null)
            {
                return;
            }

            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            if (m_BindingState != CommandHandlerUtilities.BindingState.Bound)
            {
                throw new InvalidOperationException("Cannot invoke an unbound CommandHandlerFunctor.");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, m_HandlerParam3, m_HandlerParam4, m_HandlerParam5, tCommand);
        }
    }
}
