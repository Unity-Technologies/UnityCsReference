// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.CSO;

namespace Unity.Timeline.Foundation.ViewModel
{
    static class ViewModelExtensions
    {
        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The viewmodel onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public static void RegisterCommandHandler<TCommand>(this ViewModelBase self, CommandHandler<TCommand> commandHandler)
            where TCommand : ICommand
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TCommand>(commandHandler));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The viewmodel onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TComponent">The type of the command handler first component.</typeparam>
        public static void RegisterCommandHandler<TComponent, TCommand>(this ViewModelBase self,
            CommandHandler<TComponent, TCommand> commandHandler)
            where TCommand : ICommand where TComponent : Component
        {
            self.RegisterCommandHandler<TCommand>(new CommandHandlerFunctor<TComponent, TCommand>(commandHandler,
                self.GetComponent<TComponent>()));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The viewmodel onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TComponent1">The type of the command handler first component.</typeparam>
        /// <typeparam name="TComponent2">The type of the command handler second component.</typeparam>
        public static void RegisterCommandHandler<TComponent1, TComponent2, TCommand>(this ViewModelBase self,
            CommandHandler<TComponent1, TComponent2, TCommand> commandHandler)
            where TCommand : ICommand where TComponent1 : Component where TComponent2 : Component
        {
            self.RegisterCommandHandler<TCommand>(
                new CommandHandlerFunctor<TComponent1, TComponent2, TCommand>(commandHandler,
                    self.GetComponent<TComponent1>(),
                    self.GetComponent<TComponent2>()));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The viewmodel onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TComponent1">The type of the command handler first component.</typeparam>
        /// <typeparam name="TComponent2">The type of the command handler second component.</typeparam>
        /// <typeparam name="TComponent3">The type of the command handler third component.</typeparam>
        public static void RegisterCommandHandler<TComponent1, TComponent2, TComponent3, TCommand>(this ViewModelBase self,
            CommandHandler<TComponent1, TComponent2, TComponent3, TCommand> commandHandler)
            where TCommand : ICommand
            where TComponent1 : Component where TComponent2 : Component where TComponent3 : Component
        {
            self.RegisterCommandHandler<TCommand>(
                new CommandHandlerFunctor<TComponent1, TComponent2, TComponent3, TCommand>(commandHandler,
                    self.GetComponent<TComponent1>(),
                    self.GetComponent<TComponent2>(),
                    self.GetComponent<TComponent3>()));
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="self">The viewmodel onto which to register the command handler.</param>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TComponent1">The type of the command handler first component.</typeparam>
        /// <typeparam name="TComponent2">The type of the command handler second component.</typeparam>
        /// <typeparam name="TComponent3">The type of the command handler third component.</typeparam>
        /// <typeparam name="TComponent4">The type of the command handler fourth component.</typeparam>
        public static void RegisterCommandHandler<TComponent1, TComponent2, TComponent3, TComponent4, TCommand>(this ViewModelBase self,
            CommandHandler<TComponent1, TComponent2, TComponent3, TComponent4, TCommand> commandHandler)
            where TCommand : ICommand
            where TComponent1 : Component where TComponent2 : Component where TComponent3 : Component where TComponent4 : Component
        {
            self.RegisterCommandHandler<TCommand>(
                new CommandHandlerFunctor<TComponent1, TComponent2, TComponent3, TComponent4, TCommand>(commandHandler,
                    self.GetComponent<TComponent1>(),
                    self.GetComponent<TComponent2>(),
                    self.GetComponent<TComponent3>(),
                    self.GetComponent<TComponent4>()));
        }
    }
}
