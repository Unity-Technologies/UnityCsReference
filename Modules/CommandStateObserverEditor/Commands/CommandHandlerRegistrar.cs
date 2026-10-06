// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Bindings;

namespace Unity.CSO.Editor
{
    /// <summary>
    /// Helper class to register command handlers. It will automatically bind the command handler to the
    /// state components it needs by matching their type. This class can only be used if
    /// the command handler parameters types all derive from <see cref="IStateComponent"/>
    /// and the command handler parameters types are all different.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerRegistrar : Unity.CSO.CommandHandlerRegistrar
    {
        public CommandHandlerRegistrar(ICommandTarget commandTarget)
            : base(commandTarget) { }

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
    }
}
