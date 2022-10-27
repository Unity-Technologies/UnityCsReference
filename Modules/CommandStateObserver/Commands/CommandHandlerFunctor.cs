// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.CommandStateObserver
{
    delegate void CommandHandler<in TCommand>(TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="param">The parameter to pass to the handler.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TParam">The type of the handler parameter.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    delegate void CommandHandler<in TParam, in TCommand>(TParam param, TCommand command)
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
    delegate void CommandHandler<in TParam1, in TParam2, in TCommand>(TParam1 param1, TParam2 param2, TCommand command)
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
    delegate void CommandHandler<in TParam1, in TParam2, in TParam3, in TCommand>(TParam1 param1, TParam2 param2, TParam3 param3, TCommand command)
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
    delegate void CommandHandler<in TParam1, in TParam2, in TParam3, in TParam4, in TCommand>(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// Interface to wrap a command handler, bind its parameters and invoke it.
    /// </summary>
    interface ICommandHandlerFunctor
    {
        void Invoke(ICommand command, bool logHandler);
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TCommand}"/> and invoke it.
    /// </summary>
    class CommandHandlerFunctor<TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandler<TCommand> m_Callback;

        public CommandHandlerFunctor(CommandHandler<TCommand> callback)
        {
            m_Callback = callback;
        }

        public void Invoke(ICommand command, bool logHandler)
        {
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
    class CommandHandlerFunctor<TParam, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandler<TParam, TCommand> m_Callback;
        TParam m_HandlerParam;

        public CommandHandlerFunctor(CommandHandler<TParam, TCommand> callback, TParam handlerParam)
        {
            m_Callback = callback;
            m_HandlerParam = handlerParam;
        }

        public void Invoke(ICommand command, bool logHandler)
        {
            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    class CommandHandlerFunctor<TParam1, TParam2, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandler<TParam1, TParam2, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;

        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TCommand> callback, TParam1 handlerParam1, TParam2 handlerParam2)
        {
            m_Callback = callback;
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
        }

        public void Invoke(ICommand command, bool logHandler)
        {
            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    class CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandler<TParam1, TParam2, TParam3, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;
        TParam3 m_HandlerParam3;

        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TCommand> callback, TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3)
        {
            m_Callback = callback;
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
            m_HandlerParam3 = handlerParam3;
        }

        public void Invoke(ICommand command, bool logHandler)
        {
            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, m_HandlerParam3, tCommand);
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TParam4, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    class CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand> : ICommandHandlerFunctor
        where TCommand : ICommand
    {
        CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> m_Callback;
        TParam1 m_HandlerParam1;
        TParam2 m_HandlerParam2;
        TParam3 m_HandlerParam3;
        TParam4 m_HandlerParam4;

        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> callback, TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3, TParam4 handlerParam4)
        {
            m_Callback = callback;
            m_HandlerParam1 = handlerParam1;
            m_HandlerParam2 = handlerParam2;
            m_HandlerParam3 = handlerParam3;
            m_HandlerParam4 = handlerParam4;
        }

        public void Invoke(ICommand command, bool logHandler)
        {
            if (logHandler)
            {
                Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
            }

            var tCommand = (TCommand)command;
            m_Callback(m_HandlerParam1, m_HandlerParam2, m_HandlerParam3, m_HandlerParam4, tCommand);
        }
    }
}
