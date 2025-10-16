// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Helper class to register command handlers. It will automatically bind the command handler to the
    /// state components it needs by matching their type. This class can only be used if
    /// the command handler parameters types all derive from <see cref="IStateComponent"/>
    /// and the command handler parameters types are all different.
    /// </summary>
    [UnityRestricted]
    internal class CommandHandlerRegistrar
    {
        public ICommandTarget CommandTarget { get; }
        Dictionary<Type, IStateComponent> m_StateComponents = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerRegistrar"/> class.
        /// </summary>
        /// <param name="commandTarget">The object on which to register the command handler.</param>
        public CommandHandlerRegistrar(ICommandTarget commandTarget)
        {
            CommandTarget = commandTarget;
        }

        /// <summary>
        /// Adds a <see cref="IStateComponent"/> to the registrar.
        /// </summary>
        /// <param name="stateComponent">The state component to add.</param>
        /// <typeparam name="TStateComponent">The type of the state component to add.</typeparam>
        /// <remarks>This is used to bind the command handler to the state component. The registrar does not support adding more than one state component of a given type.</remarks>
        public void AddStateComponent<TStateComponent>(TStateComponent stateComponent)
            where TStateComponent : IStateComponent
        {
            if (m_StateComponents.ContainsKey(typeof(TStateComponent)))
            {
                if (ReferenceEquals(m_StateComponents[typeof(TStateComponent)], stateComponent))
                {
                    // Don't add the same state component twice, and don't throw.
                    return;
                }

                if (m_StateComponents[typeof(TStateComponent)] == null)
                {
                    // Allow replacing a null entry.
                    m_StateComponents[typeof(TStateComponent)] = stateComponent;
                    return;
                }
            }

            m_StateComponents.Add(typeof(TStateComponent), stateComponent);
        }

        /// <summary>
        /// Registers the default command handler for a command type.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command to register the default command handler for.</typeparam>
        /// <remarks>
        /// The default command handler is a public static method named 'DefaultCommandHandler' defined in the command type.
        /// </remarks>
        public void RegisterDefaultCommandHandler<TCommand>()
        {
            List<MethodInfo> candidateMethods = new();
            CommandHandlerUtilities.GetDefaultCommandHandlerCandidates<TCommand>(candidateMethods);

            Type handlerType = null;
            Type functorType = null;
            foreach (var methodInfo in candidateMethods)
            {
                var methodParams = methodInfo.GetParameters();
                if (methodParams[^1].ParameterType == typeof(TCommand) && methodParams.Length <= 6)
                {
                    switch (methodParams.Length)
                    {
                        case 1:
                            handlerType = typeof(CommandHandler<>).MakeGenericType(typeof(TCommand));
                            functorType = typeof(CommandHandlerFunctor<>).MakeGenericType(typeof(TCommand));
                            break;

                        case 2:
                            handlerType = typeof(CommandHandler<,>).MakeGenericType(methodParams[0].ParameterType, typeof(TCommand));
                            functorType = typeof(CommandHandlerFunctor<,>).MakeGenericType(methodParams[0].ParameterType, typeof(TCommand));
                            break;

                        case 3:
                            handlerType = typeof(CommandHandler<,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, typeof(TCommand));
                            functorType = typeof(CommandHandlerFunctor<,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, typeof(TCommand));
                            break;

                        case 4:
                            handlerType = typeof(CommandHandler<,,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, methodParams[2].ParameterType, typeof(TCommand));
                            functorType = typeof(CommandHandlerFunctor<,,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, methodParams[2].ParameterType, typeof(TCommand));
                            break;

                        case 5:
                            handlerType = typeof(CommandHandler<,,,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, methodParams[2].ParameterType, methodParams[3].ParameterType, typeof(TCommand));
                            functorType = typeof(CommandHandlerFunctor<,,,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, methodParams[2].ParameterType, methodParams[3].ParameterType, typeof(TCommand));
                            break;

                        case 6:
                            handlerType = typeof(CommandHandler<,,,,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, methodParams[2].ParameterType, methodParams[3].ParameterType, methodParams[4].ParameterType, typeof(TCommand));
                            functorType = typeof(CommandHandlerFunctor<,,,,,>).MakeGenericType(methodParams[0].ParameterType, methodParams[1].ParameterType, methodParams[2].ParameterType, methodParams[3].ParameterType, methodParams[4].ParameterType, typeof(TCommand));
                            break;
                    }

                    if (handlerType != null)
                    {
                        var handlerInstance = Delegate.CreateDelegate(handlerType, methodInfo);
                        var functorInstance = (ICommandHandlerFunctor)Activator.CreateInstance(functorType, handlerInstance);
                        RegisterCommandHandlerFunctor(functorInstance);
                        break;
                    }
                }
            }

            if (handlerType == null)
            {
                throw new InvalidOperationException($"No default command handler found for command type {typeof(TCommand)}");
            }
        }

        /// <summary>
        /// Registers a command handler that receives only the command as its parameter to be passed to the command handler functor.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <typeparam name="TCommand">The type of the command to register the handler for.</typeparam>
        /// <remarks>
        /// Use this method to associate a command handler with a command type and provide more flexibility for handling commands beyond the default handler
        /// (see: <see cref="RegisterDefaultCommandHandler{TCommand}"/>). It enables specialized or context-specific command handling. A corresponding command handler
        /// functor is created and registered for this handler (see: <see cref="RegisterCommandHandlerFunctor"/>).
        /// </remarks>
        public void RegisterCommandHandler<TCommand>(CommandHandler<TCommand> callback)
            where TCommand : ICommand
        {
            var f = new CommandHandlerFunctor<TCommand>(callback);
            RegisterCommandHandlerFunctor(f);
        }

        /// <summary>
        /// Registers a command handler that accepts a command along with one additional parameter to be passed to the command handler functor.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <typeparam name="TParam1">The type of the first parameter of the command handler functor.</typeparam>
        /// <typeparam name="TCommand">The type of the command to register the handler for.</typeparam>
        /// <remarks>
        /// This method registers a command handler that takes the command and one parameter, which represents the state component affected by the command.
        /// A corresponding command handler functor is created and registered for this handler (see: <see cref="RegisterCommandHandlerFunctor"/>).
        /// </remarks>
        public void RegisterCommandHandler<TParam1, TCommand>(CommandHandler<TParam1, TCommand> callback)
            where TCommand : ICommand
        {
            var f = new CommandHandlerFunctor<TParam1, TCommand>(callback);
            RegisterCommandHandlerFunctor(f);
        }

        /// <summary>
        /// Registers a command handler that accepts a command along with two additional parameters to be passed to the command handler functor.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <typeparam name="TParam1">The type of the first parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam2">The type of the second parameter of the command handler functor.</typeparam>
        /// <typeparam name="TCommand">The type of the command to register the handler for.</typeparam>
        /// <remarks>
        /// This method registers a command handler that takes the command and two parameters, which represents the state components affected by the command.
        /// A corresponding command handler functor is created and registered for this handler (see: <see cref="RegisterCommandHandlerFunctor"/>).
        /// </remarks>
        public void RegisterCommandHandler<TParam1, TParam2, TCommand>(CommandHandler<TParam1, TParam2, TCommand> callback)
            where TCommand : ICommand
        {
            var f = new CommandHandlerFunctor<TParam1, TParam2, TCommand>(callback);
            RegisterCommandHandlerFunctor(f);
        }

        /// <summary>
        /// Registers a command handler that accepts a command along with three additional parameters to be passed to the command handler functor.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <typeparam name="TParam1">The type of the first parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam2">The type of the second parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam3">The type of the third parameter of the command handler functor.</typeparam>
        /// <typeparam name="TCommand">The type of the command to register the handler for.</typeparam>
        /// <remarks>
        /// This method registers a command handler that takes the command and three parameters, which represents the state components affected by the command.
        /// A corresponding command handler functor is created and registered for this handler (see: <see cref="RegisterCommandHandlerFunctor"/>).
        /// </remarks>
        public void RegisterCommandHandler<TParam1, TParam2, TParam3, TCommand>(CommandHandler<TParam1, TParam2, TParam3, TCommand> callback)
            where TCommand : ICommand
        {
            var f = new CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand>(callback);
            RegisterCommandHandlerFunctor(f);
        }

        /// <summary>
        /// Registers a command handler that accepts a command along with four additional parameters to be passed to the command handler functor.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <typeparam name="TParam1">The type of the first parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam2">The type of the second parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam3">The type of the third parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam4">The type of the fourth parameter of the command handler functor.</typeparam>
        /// <typeparam name="TCommand">The type of the command to register the handler for.</typeparam>
        /// <remarks>
        /// This method registers a command handler that takes the command and four parameters, which represents the state components affected by the command.
        /// A corresponding command handler functor is created and registered for this handler (see: <see cref="RegisterCommandHandlerFunctor"/>).
        /// </remarks>
        public void RegisterCommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand>(CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> callback)
            where TCommand : ICommand
        {
            var f = new CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand>(callback);
            RegisterCommandHandlerFunctor(f);
        }

        /// <summary>
        /// Registers a command handler that accepts a command along with five additional parameters to be passed to the command handler functor.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <typeparam name="TParam1">The type of the first parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam2">The type of the second parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam3">The type of the third parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam4">The type of the fourth parameter of the command handler functor.</typeparam>
        /// <typeparam name="TParam5">The type of the fifth parameter of the command handler functor.</typeparam>
        /// <typeparam name="TCommand">The type of the command to register the handler for.</typeparam>
        /// <remarks>
        /// This method registers a command handler that takes the command and five parameters, which represents the state components affected by the command.
        /// A corresponding command handler functor is created and registered for this handler (see: <see cref="RegisterCommandHandlerFunctor"/>).
        /// </remarks>
        public void RegisterCommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand>(CommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand> callback)
            where TCommand : ICommand
        {
            var f = new CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand>(callback);
            RegisterCommandHandlerFunctor(f);
        }

        /// <summary>
        /// Registers a command handler functor.
        /// </summary>
        /// <param name="commandHandlerFunctor">The command handler functor to register.</param>
        /// <remarks>
        /// The command handler functor is created from the command handler and associated state components. This method registers the functor as the handler for the command type.
        /// A functor is an object that acts like a function and determines which function to invoke and the parameters to provide. These parameters correspond to the state components affected by the command.
        /// When the command is dispatched, the functor executes the command handler and passes the relevant state components as arguments.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the command handler cannot be bound to the state components.
        /// </exception>
        public void RegisterCommandHandlerFunctor(ICommandHandlerFunctor commandHandlerFunctor)
        {
            var chType = commandHandlerFunctor.GetType();
            if (chType.IsGenericType)
            {
                var chArguments = chType.GetGenericArguments();
                var argList = new List<IStateComponent>(chArguments.Length);

                for (var i = 0; i < chArguments.Length - 1; i++)
                {
                    var argument = chArguments[i];

                    if (!m_StateComponents.TryGetValue(argument, out var component))
                    {
                        throw new InvalidOperationException($"Command handler functor {chType} is missing a state component for type {argument}");
                    }

                    argList.Add(component);
                }

                commandHandlerFunctor.Bind(argList);
                CommandTarget.RegisterCommandHandler(commandHandlerFunctor);
            }
            else
            {
                throw new InvalidOperationException($"Command handler functor {chType} is not a generic type");
            }
        }
    }
}
