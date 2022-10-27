// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.CommandStateObserver
{
    static class CommandTargetExtensions
    {
        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The dispatcher onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public static void RegisterCommandHandler<TCommand>(this ICommandTarget self, CommandHandler<TCommand> commandHandler)
            where TCommand : ICommand
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TCommand>(commandHandler));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The dispatcher onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="handlerParam">The first parameter of the command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TParam">The type of the command handler first parameter.</typeparam>
        public static void RegisterCommandHandler<TParam, TCommand>(this ICommandTarget self, CommandHandler<TParam, TCommand> commandHandler, TParam handlerParam)
            where TCommand : ICommand
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TParam, TCommand>(commandHandler, handlerParam));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The dispatcher onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="handlerParam1">The first parameter of the command handler.</param>
        /// <param name="handlerParam2">The second parameter of the command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TParam1">The type of the command handler first parameter.</typeparam>
        /// <typeparam name="TParam2">The type of the command handler second parameter.</typeparam>
        public static void RegisterCommandHandler<TParam1, TParam2, TCommand>(this ICommandTarget self, CommandHandler<TParam1, TParam2, TCommand> commandHandler, TParam1 handlerParam1, TParam2 handlerParam2)
            where TCommand : ICommand
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TParam1, TParam2, TCommand>(commandHandler, handlerParam1, handlerParam2));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The dispatcher onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="handlerParam1">The first parameter of the command handler.</param>
        /// <param name="handlerParam2">The second parameter of the command handler.</param>
        /// <param name="handlerParam3">The third parameter of the command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TParam1">The type of the command handler first parameter.</typeparam>
        /// <typeparam name="TParam2">The type of the command handler second parameter.</typeparam>
        /// <typeparam name="TParam3">The type of the command handler third parameter.</typeparam>
        public static void RegisterCommandHandler<TParam1, TParam2, TParam3, TCommand>(this ICommandTarget self, CommandHandler<TParam1, TParam2, TParam3, TCommand> commandHandler, TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3)
            where TCommand : ICommand
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand>(commandHandler, handlerParam1, handlerParam2, handlerParam3));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The dispatcher onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <param name="handlerParam1">The first parameter of the command handler.</param>
        /// <param name="handlerParam2">The second parameter of the command handler.</param>
        /// <param name="handlerParam3">The third parameter of the command handler.</param>
        /// <param name="handlerParam4">The third parameter of the command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TParam1">The type of the command handler first parameter.</typeparam>
        /// <typeparam name="TParam2">The type of the command handler second parameter.</typeparam>
        /// <typeparam name="TParam3">The type of the command handler third parameter.</typeparam>
        /// <typeparam name="TParam4">The type of the command handler third parameter.</typeparam>
        public static void RegisterCommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand>(this ICommandTarget self, CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> commandHandler, TParam1 handlerParam1, TParam2 handlerParam2, TParam3 handlerParam3, TParam4 handlerParam4)
            where TCommand : ICommand
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand>(commandHandler, handlerParam1, handlerParam2, handlerParam3, handlerParam4));
        }
    }
}
