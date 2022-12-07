// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Interface for the target of a command.
    /// </summary>
    interface ICommandTarget
    {
        /// <summary>
        /// Dispatches a command to this target.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostic flags for the dispatch process.</param>
        void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None);

        /// <summary>
        /// Registers a handler for a command type. Replaces any previously registered handler for the command type.
        /// </summary>
        /// <param name="commandHandlerFunctor">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        void RegisterCommandHandler<TCommand>(ICommandHandlerFunctor commandHandlerFunctor) where TCommand : ICommand;

        /// <summary>
        /// Unregisters the command handler for a command type.
        /// </summary>
        /// <remarks>
        /// Since there is only one command handler registered for a command type, it is not necessary
        /// to specify the command handler to unregister.
        /// </remarks>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void UnregisterCommandHandler<TCommand>() where TCommand : ICommand;
    }
}
